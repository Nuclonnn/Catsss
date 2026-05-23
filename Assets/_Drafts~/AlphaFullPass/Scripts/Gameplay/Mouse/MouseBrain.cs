using System;
using Catsss.Configs;
using Catsss.Core.FSM;
using Catsss.Core.Events;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

namespace Catsss.Gameplay.Mouse
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MouseBrain : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private EmptyEventChannel mouseCaughtChannel;
        [SerializeField] private LayerMask physicalLayerMask;
        [SerializeField] private LayerMask spectralLayerMask;

        private readonly StateMachine _stateMachine = new();
        private WanderMouseState _wanderState;
        private ChaseMouseState _chaseState;
        private TrappedMouseState _trappedState;
        private SplineContainer _activeRoute;
        private bool _isTrapped;

        public event Action Materialized;
        public event Action Caught;

        public GameConfig Config => gameConfig;
        public NavMeshAgent Agent => navMeshAgent;

        private void Awake()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            _wanderState = new WanderMouseState(this);
            _chaseState = new ChaseMouseState(this);
            _trappedState = new TrappedMouseState(this);
            _stateMachine.SetState(_wanderState);
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _stateMachine.Update();
        }

        public void StartChase(SplineContainer route)
        {
            if (!IsServer || route == null)
            {
                return;
            }

            _activeRoute = route;
            _chaseState.SetRoute(route);
            _stateMachine.SetState(_chaseState);
        }

        public void EnterDome()
        {
            if (!IsServer)
            {
                return;
            }

            _isTrapped = true;
            SetPhysicalLayer();
            _stateMachine.SetState(_trappedState);
            MaterializeClientRpc();
        }

        public Transform FindClosestPlayer()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            Transform closest = null;
            float closestDistance = float.MaxValue;

            foreach (PlayerController player in players)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player.transform;
                }
            }

            return closest;
        }

        public float GetClosestPlayerDistance()
        {
            Transform closest = FindClosestPlayer();
            return closest != null ? Vector3.Distance(transform.position, closest.position) : float.MaxValue;
        }

        public Vector3 EvaluateRoutePosition(SplineContainer route, float normalizedTime)
        {
            Vector3 localPosition = route.Spline.EvaluatePosition(Mathf.Clamp01(normalizedTime));
            return route.transform.TransformPoint(localPosition);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_isTrapped)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            mouseCaughtChannel?.Invoke();
            CaughtClientRpc();
        }

        [ClientRpc]
        private void MaterializeClientRpc()
        {
            Materialized?.Invoke();
        }

        [ClientRpc]
        private void CaughtClientRpc()
        {
            Caught?.Invoke();
        }

        private void SetPhysicalLayer()
        {
            if (physicalLayerMask.value == 0)
            {
                return;
            }

            gameObject.layer = FirstLayerIndex(physicalLayerMask);
        }

        private static int FirstLayerIndex(LayerMask mask)
        {
            int value = mask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    return i;
                }
            }

            return 0;
        }

        private abstract class MouseState : IState
        {
            protected readonly MouseBrain Brain;

            protected MouseState(MouseBrain brain)
            {
                Brain = brain;
            }

            public virtual void OnEnter() { }
            public virtual void OnExit() { }
            public virtual void OnUpdate() { }
            public virtual void OnFixedUpdate() { }
        }

        private sealed class WanderMouseState : MouseState
        {
            private float _nextPointTime;

            public WanderMouseState(MouseBrain brain) : base(brain)
            {
            }

            public override void OnEnter()
            {
                if (Brain.Agent != null)
                {
                    Brain.Agent.enabled = true;
                }

                PickNextPoint();
            }

            public override void OnUpdate()
            {
                if (Time.time >= _nextPointTime || Brain.Agent != null && !Brain.Agent.pathPending && Brain.Agent.remainingDistance <= Brain.Agent.stoppingDistance)
                {
                    PickNextPoint();
                }
            }

            private void PickNextPoint()
            {
                if (Brain.Agent == null || !Brain.Agent.enabled)
                {
                    return;
                }

                float radius = Brain.Config != null ? Brain.Config.Mouse.wanderRadius : 10f;
                Vector3 randomPoint = Brain.transform.position + UnityEngine.Random.insideUnitSphere * radius;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    Brain.Agent.SetDestination(hit.position);
                }

                float min = Brain.Config != null ? Brain.Config.Mouse.wanderDelayMin : 3f;
                float max = Brain.Config != null ? Brain.Config.Mouse.wanderDelayMax : 5f;
                _nextPointTime = Time.time + UnityEngine.Random.Range(min, max);
            }
        }

        private sealed class ChaseMouseState : MouseState
        {
            private SplineContainer _route;
            private float _normalizedTime;

            public ChaseMouseState(MouseBrain brain) : base(brain)
            {
            }

            public void SetRoute(SplineContainer route)
            {
                _route = route;
                _normalizedTime = 0f;
            }

            public override void OnEnter()
            {
                if (Brain.Agent != null)
                {
                    Brain.Agent.enabled = false;
                }
            }

            public override void OnUpdate()
            {
                if (_route == null)
                {
                    return;
                }

                float speed = Brain.Config != null ? Brain.Config.Mouse.chaseSpeed : 8f;
                float distanceToPlayer = Brain.GetClosestPlayerDistance();

                if (Brain.Config != null && distanceToPlayer > Brain.Config.Mouse.rubberbandFarDistance)
                {
                    speed *= Brain.Config.Mouse.rubberbandSlowMultiplier;
                }

                float routeLength = Mathf.Max(0.01f, _route.CalculateLength());
                _normalizedTime += speed * Time.deltaTime / routeLength;
                Brain.transform.position = Brain.EvaluateRoutePosition(_route, _normalizedTime);

                if (_normalizedTime >= 1f)
                {
                    Brain.EnterDome();
                }
            }
        }

        private sealed class TrappedMouseState : MouseState
        {
            public TrappedMouseState(MouseBrain brain) : base(brain)
            {
            }

            public override void OnEnter()
            {
                if (Brain.Agent != null)
                {
                    Brain.Agent.enabled = true;
                }
            }

            public override void OnUpdate()
            {
                if (Brain.Agent == null || !Brain.Agent.enabled)
                {
                    return;
                }

                Transform closestPlayer = Brain.FindClosestPlayer();
                if (closestPlayer == null)
                {
                    return;
                }

                Vector3 fleeDirection = (Brain.transform.position - closestPlayer.position).normalized;
                float fleeDistance = Brain.Config != null ? Brain.Config.Mouse.trappedFleeDistance : 6f;
                Vector3 target = Brain.transform.position + fleeDirection * fleeDistance;

                if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                {
                    Brain.Agent.SetDestination(hit.position);
                }
            }
        }
    }
}
