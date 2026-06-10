using System;
using System.Collections.Generic;
using Catsss.Configs.Mouse;
using Catsss.Core.Events;
using Catsss.Core.FSM;
using Catsss.Core.Path;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>
    /// Серверное ядро мыши: FSM состояний, репликация presence, движение по MouseRoute, купол.
    /// Логика не ссылается на Animator/VFX/Audio — только события и ClientRpc-заготовки.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MouseBrain : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] private MouseConfig config;

        [Header("Scene")]
        [SerializeField] private MouseBurrowPoint homeBurrow;
        [SerializeField] private Collider catchCollider;
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Events")]
        [SerializeField] private EmptyEventChannel mouseCaughtChannel;

        [Header("Dome")]
        [Tooltip("Радиус поиска NavMesh при входе в купол (м).")]
        [SerializeField, Min(0.5f)] private float domeNavMeshSampleRadius = 5f;

        private readonly StateMachine _stateMachine = new();
        private readonly NetworkVariable<MousePresenceMode> _presenceMode =
            new(MousePresenceMode.Hidden, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private HiddenMouseState _hiddenState;
        private RouteFollowMouseState _routeFollowState;
        private DomeFleeMouseState _domeFleeState;
        private CaughtMouseState _caughtState;
        private Rigidbody _rigidbody;
        private bool _isFollowingRoute;
        private bool _isInDomeFlee;

        public MousePresenceMode PresenceMode => _presenceMode.Value;
        public bool IsFollowingRoute => _isFollowingRoute;
        public bool IsInDomeFlee => _isInDomeFlee;
        public MouseConfig Config => config;

        public event Action<MousePresenceMode, MousePresenceMode> PresenceModeChanged;
        public event Action Materialized;
        public event Action Caught;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            ConfigureRigidbody();
            ConfigureNavMeshAgent();
            _hiddenState = new HiddenMouseState(this);
            _routeFollowState = new RouteFollowMouseState(this);
            _domeFleeState = new DomeFleeMouseState(this);
            _caughtState = new CaughtMouseState(this);
        }

        public override void OnNetworkSpawn()
        {
            _presenceMode.OnValueChanged += HandlePresenceModeChanged;

            if (IsServer)
            {
                InitializeServerState();
            }
            else
            {
                PresenceModeChanged?.Invoke(MousePresenceMode.Hidden, _presenceMode.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _presenceMode.OnValueChanged -= HandlePresenceModeChanged;
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            if (!IsServer)
            {
                return;
            }

            _stateMachine.FixedUpdate();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_isInDomeFlee)
            {
                return;
            }

            if (other.GetComponentInParent<NetworkPlayerController>() == null)
            {
                return;
            }

            _stateMachine.SetState(_caughtState);
        }

        public void PlayRouteServer(
            MouseRoute route,
            bool teleportToRouteStart = true,
            EmptyEventChannel onStartedChannel = null,
            EmptyEventChannel onFinishedChannel = null)
        {
            if (!IsServer || route == null)
            {
                return;
            }

            if (!route.IsValid(out string error))
            {
                Debug.LogWarning($"[MouseBrain] Маршрут '{route.name}' невалиден: {error}", route);
                return;
            }

            _routeFollowState.Prepare(route, teleportToRouteStart, onStartedChannel, onFinishedChannel);
            _stateMachine.SetState(_routeFollowState);
        }

        public void TeleportToHomeServer()
        {
            if (!IsServer)
            {
                return;
            }

            Vector3 position = homeBurrow != null ? homeBurrow.Position : transform.position;
            Quaternion rotation = homeBurrow != null ? homeBurrow.Rotation : transform.rotation;
            TeleportServer(position, rotation);
        }

        public void SetPresenceModeServer(MousePresenceMode mode)
        {
            if (!IsServer)
            {
                return;
            }

            _presenceMode.Value = mode;
            ApplyPhysicsLayer(mode);
            ApplyCatchCollider(mode);
        }

        /// <summary>Сервер: мышь материализуется в куполе и убегает по NavMesh.</summary>
        public void EnterDomeServer()
        {
            if (!IsServer)
            {
                return;
            }

            _stateMachine.SetState(_domeFleeState);
        }

        public void NotifyCaughtServer()
        {
            if (!IsServer)
            {
                return;
            }

            mouseCaughtChannel?.Invoke();
            CaughtClientRpc();
        }

        internal void TeleportServer(Vector3 position, Quaternion rotation)
        {
            if (_rigidbody != null)
            {
                _rigidbody.position = position;
                _rigidbody.rotation = rotation;
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }

            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.Warp(position);
            }
        }

        internal void MoveServer(Vector3 position, Quaternion rotation)
        {
            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(position);
                _rigidbody.MoveRotation(rotation);
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
            }
        }

        internal void TransitionToHiddenState(bool teleportToHomeBurrow = true)
        {
            if (!IsServer)
            {
                return;
            }

            _hiddenState.Prepare(teleportToHomeBurrow);
            _stateMachine.SetState(_hiddenState);
        }

        internal void TransitionToHiddenAtCurrentPositionServer()
        {
            TransitionToHiddenState(teleportToHomeBurrow: false);
        }

        internal void SetRouteFollowingServer(bool isFollowing)
        {
            _isFollowingRoute = isFollowing;
        }

        internal void SetDomeFleeActiveServer(bool isActive)
        {
            _isInDomeFlee = isActive;
        }

        internal Transform FindClosestPlayerTransform()
        {
            NetworkPlayerController[] players = FindObjectsByType<NetworkPlayerController>();

            Transform closest = null;
            float closestDistance = float.MaxValue;

            foreach (NetworkPlayerController player in players)
            {
                if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(transform.position - player.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = player.transform;
                }
            }

            return closest;
        }

        internal void EnableNavMeshAgentServer(bool enabled)
        {
            if (navMeshAgent == null)
            {
                return;
            }

            if (enabled)
            {
                navMeshAgent.enabled = true;
                return;
            }

            ShutdownNavMeshAgentServer();
        }

        /// <summary>Сервер: безопасно остановить agent (без ошибок вне NavMesh).</summary>
        internal void ShutdownNavMeshAgentServer()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
            {
                return;
            }

            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;
            }

            navMeshAgent.enabled = false;
        }

        /// <summary>Сервер: warp agent + rigidbody на ближайший NavMesh.</summary>
        internal bool TryPlaceAgentOnNavMeshServer(Vector3 nearPosition, out Vector3 placedPosition)
        {
            placedPosition = nearPosition;

            if (navMeshAgent == null)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(nearPosition, out NavMeshHit hit, domeNavMeshSampleRadius, NavMesh.AllAreas))
            {
                return false;
            }

            placedPosition = hit.position;
            navMeshAgent.enabled = true;
            navMeshAgent.Warp(placedPosition);
            TeleportServer(placedPosition, transform.rotation);
            return navMeshAgent.isOnNavMesh;
        }

        internal NavMeshAgent Agent => navMeshAgent;

        /// <summary>Сервер: NavMeshAgent двигает transform — синхронизируем kinematic Rigidbody для NGO.</summary>
        internal void SyncNavMeshAgentTransformServer()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            Vector3 position = navMeshAgent.nextPosition;
            Quaternion rotation = transform.rotation;

            if (navMeshAgent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 forward = navMeshAgent.velocity;
                forward.y = 0f;

                if (forward.sqrMagnitude > 0.0001f)
                {
                    rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                }
            }

            MoveServer(position, rotation);
            navMeshAgent.nextPosition = position;
        }

        internal void ConfigureNavMeshAgentForManualSyncServer()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.nextPosition = transform.position;
        }

        private void InitializeServerState()
        {
            TransitionToHiddenState(teleportToHomeBurrow: true);
        }

        private void HandlePresenceModeChanged(MousePresenceMode previous, MousePresenceMode current)
        {
            PresenceModeChanged?.Invoke(previous, current);
        }

        private void ResolveReferences()
        {
            if (_rigidbody == null)
            {
                TryGetComponent(out _rigidbody);
            }

            if (catchCollider == null)
            {
                TryGetComponent(out catchCollider);
            }

            if (navMeshAgent == null)
            {
                TryGetComponent(out navMeshAgent);
            }
        }

        private void ConfigureRigidbody()
        {
            if (!TryGetComponent(out Rigidbody body))
            {
                return;
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void ConfigureNavMeshAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.enabled = false;
            navMeshAgent.updateUpAxis = true;
        }

        private void ApplyPhysicsLayer(MousePresenceMode mode)
        {
            LayerMask mask = mode switch
            {
                MousePresenceMode.Physical => config != null ? config.PhysicalLayer : default,
                MousePresenceMode.Spectral => config != null ? config.SpectralLayer : default,
                _ => config != null ? config.SpectralLayer : default,
            };

            int layer = FirstLayerIndex(mask);

            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        private void ApplyCatchCollider(MousePresenceMode mode)
        {
            if (catchCollider == null)
            {
                return;
            }

            catchCollider.enabled = mode == MousePresenceMode.Physical;
        }

        private static int FirstLayerIndex(LayerMask mask)
        {
            int value = mask.value;

            if (value == 0)
            {
                return -1;
            }

            for (int i = 0; i < 32; i++)
            {
                if ((value & (1 << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
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

        private sealed class HiddenMouseState : IState
        {
            private readonly MouseBrain _brain;
            private bool _teleportToHomeBurrow = true;

            public HiddenMouseState(MouseBrain brain) => _brain = brain;

            public void Prepare(bool teleportToHomeBurrow)
            {
                _teleportToHomeBurrow = teleportToHomeBurrow;
            }

            public void OnEnter()
            {
                _brain.SetRouteFollowingServer(false);
                _brain.SetDomeFleeActiveServer(false);
                _brain.EnableNavMeshAgentServer(false);

                if (_teleportToHomeBurrow)
                {
                    _brain.TeleportToHomeServer();
                }

                _brain.SetPresenceModeServer(MousePresenceMode.Hidden);
            }

            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
        }

        private sealed class RouteFollowMouseState : IState
        {
            private readonly MouseBrain _brain;
            private readonly WaypointPathFollower _follower = new();

            private MouseRoute _route;
            private bool _teleportToRouteStart;
            private bool _routeFinishedHandled;
            private Vector3 _lastPosition;
            private EmptyEventChannel _onStartedChannel;
            private EmptyEventChannel _onFinishedChannel;
            private readonly HashSet<int> _firedWaypointEventIndices = new();
            private bool _isWaitingAtWaypoint;
            private float _waypointWaitRemaining;

            public RouteFollowMouseState(MouseBrain brain) => _brain = brain;

            public void Prepare(
                MouseRoute route,
                bool teleportToRouteStart,
                EmptyEventChannel onStartedChannel,
                EmptyEventChannel onFinishedChannel)
            {
                _route = route;
                _teleportToRouteStart = teleportToRouteStart;
                _onStartedChannel = onStartedChannel;
                _onFinishedChannel = onFinishedChannel;
            }

            public void OnEnter()
            {
                _routeFinishedHandled = false;
                _isWaitingAtWaypoint = false;
                _waypointWaitRemaining = 0f;
                _firedWaypointEventIndices.Clear();
                _brain.EnableNavMeshAgentServer(false);
                _brain.SetRouteFollowingServer(true);
                _brain.SetPresenceModeServer(MousePresenceMode.Spectral);

                Vector3[] points = _route.GetWaypointPositions();
                int startIndex = 0;

                if (_teleportToRouteStart)
                {
                    _brain.TeleportServer(_route.GetStartPosition(), _route.GetStartRotation());
                }
                else
                {
                    startIndex = WaypointPathSnapshot.FindClosestWaypointIndex(points, _brain.transform.position);
                }

                _follower.Begin(points, startIndex);
                _lastPosition = _brain.transform.position;
                _onStartedChannel?.Invoke();
                BeginWaypointWaitIfNeeded(TriggerWaypointEventsServer(0));
            }

            public void OnExit() => _brain.SetRouteFollowingServer(false);

            public void OnUpdate() { }

            public void OnFixedUpdate()
            {
                if (_route == null)
                {
                    return;
                }

                if (_follower.IsComplete)
                {
                    HandleRouteFinishedOnce();
                    return;
                }

                if (_isWaitingAtWaypoint)
                {
                    _waypointWaitRemaining -= Time.fixedDeltaTime;

                    if (_waypointWaitRemaining > 0f)
                    {
                        return;
                    }

                    _isWaitingAtWaypoint = false;
                    _waypointWaitRemaining = 0f;
                }

                float speed = _route.ResolveSpeed(_brain.Config);
                bool reachedWaypoint = _follower.TryAdvance(
                    Time.fixedDeltaTime,
                    speed,
                    out Vector3 nextPosition,
                    out int reachedWaypointIndex);

                Quaternion rotation = ResolveFacingRotation(nextPosition);
                _brain.MoveServer(nextPosition, rotation);
                _lastPosition = nextPosition;

                if (reachedWaypoint)
                {
                    BeginWaypointWaitIfNeeded(TriggerWaypointEventsServer(reachedWaypointIndex));
                }

                if (_follower.IsComplete)
                {
                    HandleRouteFinishedOnce();
                }
            }

            private float TriggerWaypointEventsServer(int waypointIndex)
            {
                ReadOnlySpan<MouseRouteWaypointEvent> events = _route.WaypointEvents;
                float maxWait = 0f;

                for (int i = 0; i < events.Length; i++)
                {
                    MouseRouteWaypointEvent waypointEvent = events[i];

                    if (waypointEvent == null || waypointEvent.Channel == null)
                    {
                        continue;
                    }

                    if (waypointEvent.WaypointIndex != waypointIndex)
                    {
                        continue;
                    }

                    if (waypointEvent.PlayOncePerRouteRun && _firedWaypointEventIndices.Contains(i))
                    {
                        continue;
                    }

                    waypointEvent.Channel.Invoke();
                    _firedWaypointEventIndices.Add(i);
                    maxWait = Mathf.Max(maxWait, waypointEvent.WaitSeconds);

                    if (_route.LogWaypointEvents)
                    {
                        Debug.Log(
                            $"[MouseRoute] {_route.name}: waypoint P{waypointIndex} -> {waypointEvent.Channel.name}",
                            _route);
                    }
                }

                return maxWait;
            }

            private void BeginWaypointWaitIfNeeded(float waitSeconds)
            {
                if (waitSeconds <= 0f)
                {
                    return;
                }

                _isWaitingAtWaypoint = true;
                _waypointWaitRemaining = Mathf.Max(_waypointWaitRemaining, waitSeconds);
            }

            private void HandleRouteFinishedOnce()
            {
                if (_routeFinishedHandled)
                {
                    return;
                }

                _routeFinishedHandled = true;
                _onFinishedChannel?.Invoke();

                switch (_route.EndMode)
                {
                    case MouseRouteEndMode.ReturnHidden:
                        _brain.TransitionToHiddenAtCurrentPositionServer();
                        break;
                    case MouseRouteEndMode.EnterDome:
                        _brain.EnterDomeServer();
                        break;
                    case MouseRouteEndMode.StopAtEnd:
                    default:
                        _brain.SetRouteFollowingServer(false);
                        break;
                }
            }

            private Quaternion ResolveFacingRotation(Vector3 nextPosition)
            {
                Vector3 delta = nextPosition - _lastPosition;
                delta.y = 0f;

                if (delta.sqrMagnitude < 0.0001f)
                {
                    return _brain.transform.rotation;
                }

                Quaternion target = Quaternion.LookRotation(delta.normalized, Vector3.up);
                float turnSpeed = _brain.Config != null ? _brain.Config.RouteTurnSpeedDegPerSec : 720f;
                return Quaternion.RotateTowards(
                    _brain.transform.rotation,
                    target,
                    turnSpeed * Time.fixedDeltaTime);
            }
        }

        /// <summary>Купол: NavMesh flee, поимка через trigger с игроком.</summary>
        private sealed class DomeFleeMouseState : IState
        {
            private readonly MouseBrain _brain;

            public DomeFleeMouseState(MouseBrain brain) => _brain = brain;

            public void OnEnter()
            {
                _brain.SetRouteFollowingServer(false);
                _brain.SetDomeFleeActiveServer(true);
                _brain.SetPresenceModeServer(MousePresenceMode.Physical);
                MaterializeClientRpcFromServer();

                NavMeshAgent agent = _brain.Agent;

                if (agent == null)
                {
                    Debug.LogWarning("[MouseBrain] NavMeshAgent не назначен — flee в куполе не работает.", _brain);
                    _brain.SetDomeFleeActiveServer(false);
                    return;
                }

                if (!_brain.TryPlaceAgentOnNavMeshServer(_brain.transform.position, out _))
                {
                    Debug.LogWarning(
                        $"[MouseBrain] NavMesh не найден рядом с мышью ({_brain.transform.position}). " +
                        $"Запеките NavMesh под куполом и/или поставьте Home Burrow на платформе. Радиус поиска: {_brain.domeNavMeshSampleRadius} м.",
                        _brain);
                    _brain.ShutdownNavMeshAgentServer();
                    _brain.SetDomeFleeActiveServer(false);
                    return;
                }

                float speed = _brain.Config != null ? _brain.Config.DomeAgentSpeed : 3.5f;
                agent.speed = speed;
                _brain.ConfigureNavMeshAgentForManualSyncServer();

                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }

            public void OnExit()
            {
                _brain.SetDomeFleeActiveServer(false);
                _brain.ShutdownNavMeshAgentServer();
            }

            public void OnUpdate() { }

            public void OnFixedUpdate()
            {
                NavMeshAgent agent = _brain.Agent;

                if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                {
                    return;
                }

                Transform closestPlayer = _brain.FindClosestPlayerTransform();

                if (closestPlayer == null)
                {
                    return;
                }

                float fleeDistance = _brain.Config != null ? _brain.Config.DomeFleeDistance : 6f;
                Vector3 fleeDirection = (_brain.transform.position - closestPlayer.position).normalized;
                fleeDirection.y = 0f;

                if (fleeDirection.sqrMagnitude < 0.0001f)
                {
                    fleeDirection = _brain.transform.forward;
                }

                Vector3 target = _brain.transform.position + fleeDirection.normalized * fleeDistance;

                if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }

                _brain.SyncNavMeshAgentTransformServer();
            }

            private void MaterializeClientRpcFromServer()
            {
                _brain.MaterializeClientRpc();
            }
        }

        private sealed class CaughtMouseState : IState
        {
            private readonly MouseBrain _brain;

            public CaughtMouseState(MouseBrain brain) => _brain = brain;

            public void OnEnter()
            {
                _brain.SetDomeFleeActiveServer(false);
                _brain.ShutdownNavMeshAgentServer();
                _brain.NotifyCaughtServer();
            }

            public void OnExit() { }
            public void OnUpdate() { }
            public void OnFixedUpdate() { }
        }
    }
}
