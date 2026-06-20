using System;
using Catsss.Configs.Mouse;
using Catsss.Core.Events;
using Catsss.Core.FSM;
using Catsss.Gameplay.Mouse.States;
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
    public sealed class MouseBrain : NetworkBehaviour, IMouseBrainStateHost
    {
        [Header("Config")]
        [SerializeField] private MouseConfig config;

        [Header("Scene")]
        [SerializeField] private MouseBurrowPoint homeBurrow;
        [SerializeField] private Collider catchCollider;
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Events")]
        [SerializeField] private EmptyEventChannel mouseCaughtChannel;

        private readonly StateMachine _stateMachine = new();
        private readonly MouseBrainTransitionSignals _transitionSignals = new();
        private readonly NetworkVariable<MousePresenceMode> _presenceMode =
            new(MousePresenceMode.Hidden, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private MouseBrainStateSet _states;
        private Rigidbody _rigidbody;
        private bool _isFollowingRoute;
        private bool _isInDomeFlee;

        Transform IMouseBrainStateHost.Transform => transform;

        MouseConfig IMouseBrainStateHost.Config => config;

        MouseBrainTransitionSignals IMouseBrainStateHost.TransitionSignals => _transitionSignals;

        float IMouseBrainStateHost.DomeNavMeshSampleRadius =>
            config != null ? config.DomeNavMeshSampleRadius : 5f;

        NavMeshAgent IMouseBrainStateHost.Agent => navMeshAgent;

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
            _states = new MouseBrainStateSet(this);
            MouseBrainFsmTransitions.Configure(_stateMachine, _states, _transitionSignals);
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

            _transitionSignals.RequestCatch();
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

            _states.RouteFollow.Prepare(route, teleportToRouteStart, onStartedChannel, onFinishedChannel);
            _stateMachine.SetState(_states.RouteFollow);
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

            _stateMachine.SetState(_states.DomeFlee);
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

        void IMouseBrainStateHost.TeleportToHomeServer() => TeleportToHomeServer();

        void IMouseBrainStateHost.TeleportServer(Vector3 position, Quaternion rotation) => TeleportServer(position, rotation);

        void IMouseBrainStateHost.MoveServer(Vector3 position, Quaternion rotation) => MoveServer(position, rotation);

        void IMouseBrainStateHost.SetPresenceModeServer(MousePresenceMode mode) => SetPresenceModeServer(mode);

        void IMouseBrainStateHost.SetRouteFollowingServer(bool isFollowing) => SetRouteFollowingServer(isFollowing);

        void IMouseBrainStateHost.SetDomeFleeActiveServer(bool isActive) => SetDomeFleeActiveServer(isActive);

        void IMouseBrainStateHost.EnableNavMeshAgentServer(bool enabled) => EnableNavMeshAgentServer(enabled);

        void IMouseBrainStateHost.ShutdownNavMeshAgentServer() => ShutdownNavMeshAgentServer();

        Transform IMouseBrainStateHost.FindClosestPlayerTransform() => FindClosestPlayerTransform();

        bool IMouseBrainStateHost.TryPlaceAgentOnNavMeshServer(Vector3 nearPosition, out Vector3 placedPosition) =>
            TryPlaceAgentOnNavMeshServer(nearPosition, out placedPosition);

        void IMouseBrainStateHost.SyncNavMeshAgentTransformServer() => SyncNavMeshAgentTransformServer();

        void IMouseBrainStateHost.ConfigureNavMeshAgentForManualSyncServer() => ConfigureNavMeshAgentForManualSyncServer();

        void IMouseBrainStateHost.NotifyMaterializedClients() => MaterializeClientRpc();

        void IMouseBrainStateHost.NotifyCaughtServer() => NotifyCaughtServer();

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

            _states.Hidden.Prepare(teleportToHomeBurrow);
            _stateMachine.SetState(_states.Hidden);
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

            if (!NavMesh.SamplePosition(
                    nearPosition,
                    out NavMeshHit hit,
                    ((IMouseBrainStateHost)this).DomeNavMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            placedPosition = hit.position;
            navMeshAgent.enabled = true;
            navMeshAgent.Warp(placedPosition);
            TeleportServer(placedPosition, transform.rotation);
            return navMeshAgent.isOnNavMesh;
        }

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
    }
}
