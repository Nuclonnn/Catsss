using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Серверная кинематическая платформа: waypoints задаются child Transform'ами под Path,
    /// на spawn позиции кэшируются в world space. Root (с collider) двигается по кэшу.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KinematicPlatform : NetworkBehaviour
    {
        [Header("Path")]
        [Tooltip("Дочерний Path с Point_0, Point_1... Точки используются только для настройки и snapshot на spawn.")]
        [SerializeField] private Transform pathRoot;

        [Header("Mode")]
        [SerializeField] private KinematicPlatformActivationMode activationMode = KinematicPlatformActivationMode.Cycle;
        [SerializeField] private KinematicPlatformLoopMode loopMode = KinematicPlatformLoopMode.Yoyo;

        [Header("Motion")]
        [SerializeField] private KinematicPlatformSpeedMode speedMode = KinematicPlatformSpeedMode.TimePerSegment;
        [SerializeField, Min(0.01f)] private float moveTimePerSegment = 2f;
        [SerializeField, Min(0.01f)] private float unitsPerSecond = 2f;
        [SerializeField] private KinematicPlatformEasing easing = KinematicPlatformEasing.SmoothStep;

        [Header("Debug")]
        [SerializeField] private bool drawPathGizmo = true;
        [SerializeField] private bool enableDebugLogs;

        private readonly Vector3[] _emptyPath = Array.Empty<Vector3>();

        private Vector3[] _cachedWaypoints = Array.Empty<Vector3>();
        private Rigidbody _rigidbody;
        private Vector3 _previousPosition;
        private int _currentWaypointIndex;
        private int _direction = 1;
        private float _segmentProgress;
        private bool _isMoving;
        private bool _continueToEndThenStop;
        private bool _stopAtTerminal;
        private bool _signalTravelActive;
        private KinematicPlatformSignalTravelMode _signalTravelMode = KinematicPlatformSignalTravelMode.OneWay;
        private bool _returningByPolicy;
        private int _returnTargetIndex;
        private bool _pathCached;

        /// <summary>Смещение root за последний кадр (LateUpdate) — после интерполяции ServerNetworkTransform.</summary>
        public Vector3 PlatformDelta { get; private set; }

        public bool IsMoving => _isMoving;
        public IReadOnlyList<Vector3> CachedWaypoints => _cachedWaypoints;
        public Transform PathRoot => pathRoot;

        private void Reset()
        {
            ConfigureRigidbody();
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            ConfigureRigidbody();
            _previousPosition = transform.position;
        }

        public override void OnNetworkSpawn()
        {
            CacheWaypointSnapshot();

            if (IsServer)
            {
                InitializeServerMotionState();
            }

            _previousPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (IsServer && _pathCached && _cachedWaypoints.Length >= 2)
            {
                TickServerMotion(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;
            PlatformDelta = currentPosition - _previousPosition;
            _previousPosition = currentPosition;
        }

        /// <summary>Сервер: команда от signal driver.</summary>
        public void ApplySignalAction(
            KinematicPlatformSignalAction action,
            KinematicPlatformSignalTravelMode travelMode = KinematicPlatformSignalTravelMode.OneWay)
        {
            if (!IsServer)
            {
                return;
            }

            ExecuteAction(action, travelMode);
        }

        /// <summary>Сервер: resonance zone сообщает, удерживается ли платформа активной.</summary>
        public void SetResonanceHeldServer(bool isHeld, KinematicPlatformEndPolicy releasePolicyWhenReleased)
        {
            if (!IsServer || activationMode != KinematicPlatformActivationMode.Resonance)
            {
                return;
            }

            if (isHeld)
            {
                _returningByPolicy = false;
                _continueToEndThenStop = false;
                _stopAtTerminal = false;
                _signalTravelActive = false;
                _direction = 1;
                _isMoving = true;
                return;
            }

            ApplyEndPolicy(releasePolicyWhenReleased);
        }

        public void PlayForwardServer()
        {
            if (IsServer)
            {
                ExecuteAction(KinematicPlatformSignalAction.PlayForward);
            }
        }

        public void PlayReverseServer()
        {
            if (IsServer)
            {
                ExecuteAction(KinematicPlatformSignalAction.PlayReverse);
            }
        }

        public void StopServer()
        {
            if (IsServer)
            {
                ExecuteAction(KinematicPlatformSignalAction.Stop);
            }
        }

        /// <summary>Editor / gizmo: world-позиции точек Path (до snapshot — live transforms).</summary>
        public Vector3[] GetEditorWaypointPositions()
        {
            if (!Application.isPlaying || !_pathCached)
            {
                return CollectLiveWaypointPositions();
            }

            return _cachedWaypoints.Length > 0 ? _cachedWaypoints : CollectLiveWaypointPositions();
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

        private void CacheWaypointSnapshot()
        {
            _cachedWaypoints = CollectLiveWaypointPositions();
            _pathCached = _cachedWaypoints.Length >= 2;

            if (!_pathCached)
            {
                Debug.LogWarning(
                    $"[KinematicPlatform] {name}: нужно минимум 2 точки под Path.",
                    this);
            }
        }

        private Vector3[] CollectLiveWaypointPositions()
        {
            if (pathRoot == null)
            {
                return _emptyPath;
            }

            int count = pathRoot.childCount;

            if (count == 0)
            {
                return _emptyPath;
            }

            Vector3[] points = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform child = pathRoot.GetChild(i);
                points[i] = child != null ? child.position : transform.position;
            }

            return points;
        }

        private void InitializeServerMotionState()
        {
            _currentWaypointIndex = FindClosestWaypointIndex(transform.position);
            _segmentProgress = 0f;
            _direction = 1;
            _isMoving = activationMode == KinematicPlatformActivationMode.Cycle;
            _continueToEndThenStop = false;
            _stopAtTerminal = false;
            _signalTravelActive = false;
            _returningByPolicy = false;
        }

        private void TickServerMotion(float deltaTime)
        {
            if (!_isMoving)
            {
                return;
            }

            if (_returningByPolicy)
            {
                MoveTowardsWaypointIndex(_returnTargetIndex, deltaTime);

                if (_currentWaypointIndex == _returnTargetIndex && _segmentProgress <= 0.001f)
                {
                    _segmentProgress = 0f;
                    _isMoving = false;
                    _returningByPolicy = false;
                }

                return;
            }

            AdvanceAlongPath(deltaTime);

            if (_stopAtTerminal && HasReachedTerminalForDirection())
            {
                _isMoving = false;
                _stopAtTerminal = false;
                _signalTravelActive = false;
            }

            if (_continueToEndThenStop && HasReachedTerminalForDirection())
            {
                _isMoving = false;
                _continueToEndThenStop = false;
            }
        }

        private void AdvanceAlongPath(float deltaTime)
        {
            int nextIndex = GetNextWaypointIndex();

            if (nextIndex == _currentWaypointIndex)
            {
                return;
            }

            float segmentLength = Vector3.Distance(
                _cachedWaypoints[_currentWaypointIndex],
                _cachedWaypoints[nextIndex]);

            if (segmentLength < 0.001f)
            {
                _currentWaypointIndex = nextIndex;
                _segmentProgress = 0f;
                return;
            }

            if (speedMode == KinematicPlatformSpeedMode.TimePerSegment)
            {
                _segmentProgress += deltaTime / moveTimePerSegment;
            }
            else
            {
                float progressDelta = (unitsPerSecond * deltaTime) / segmentLength;
                _segmentProgress += progressDelta;
            }

            while (_segmentProgress >= 1f)
            {
                _segmentProgress -= 1f;
                _currentWaypointIndex = nextIndex;
                nextIndex = GetNextWaypointIndex();

                if (nextIndex == _currentWaypointIndex)
                {
                    _segmentProgress = 0f;
                    break;
                }

                segmentLength = Vector3.Distance(
                    _cachedWaypoints[_currentWaypointIndex],
                    _cachedWaypoints[nextIndex]);

                if (segmentLength < 0.001f)
                {
                    _segmentProgress = 0f;
                    break;
                }
            }

            float eased = ApplyEasing(Mathf.Clamp01(_segmentProgress));
            Vector3 from = _cachedWaypoints[_currentWaypointIndex];
            Vector3 to = _cachedWaypoints[nextIndex];
            Vector3 nextPosition = Vector3.LerpUnclamped(from, to, eased);
            _rigidbody.MovePosition(nextPosition);
        }

        private void MoveTowardsWaypointIndex(int targetIndex, float deltaTime)
        {
            targetIndex = Mathf.Clamp(targetIndex, 0, _cachedWaypoints.Length - 1);

            if (_currentWaypointIndex == targetIndex && _segmentProgress <= 0.001f)
            {
                _rigidbody.MovePosition(_cachedWaypoints[targetIndex]);
                return;
            }

            if (_currentWaypointIndex < targetIndex)
            {
                _direction = 1;
            }
            else if (_currentWaypointIndex > targetIndex)
            {
                _direction = -1;
            }
            else
            {
                _direction = 1;
            }

            AdvanceAlongPath(deltaTime);
        }

        private int GetNextWaypointIndex()
        {
            int lastIndex = _cachedWaypoints.Length - 1;

            if (lastIndex <= 0)
            {
                return _currentWaypointIndex;
            }

            if (UsesPathLoop())
            {
                return (_currentWaypointIndex + _direction + _cachedWaypoints.Length) % _cachedWaypoints.Length;
            }

            int candidate = _currentWaypointIndex + _direction;

            if (candidate < 0 || candidate > lastIndex)
            {
                if (UsesPathYoyo())
                {
                    _direction *= -1;
                    candidate = _currentWaypointIndex + _direction;
                }
                else
                {
                    return _currentWaypointIndex;
                }
            }

            return Mathf.Clamp(candidate, 0, lastIndex);
        }

        private bool UsesPathLoop()
        {
            if (activationMode == KinematicPlatformActivationMode.Cycle)
            {
                return loopMode == KinematicPlatformLoopMode.Loop;
            }

            return activationMode == KinematicPlatformActivationMode.SignalDriven
                && _signalTravelActive
                && _signalTravelMode == KinematicPlatformSignalTravelMode.Loop;
        }

        private bool UsesPathYoyo()
        {
            if (activationMode == KinematicPlatformActivationMode.Cycle)
            {
                return loopMode == KinematicPlatformLoopMode.Yoyo;
            }

            return activationMode == KinematicPlatformActivationMode.SignalDriven
                && _signalTravelActive
                && _signalTravelMode == KinematicPlatformSignalTravelMode.Yoyo;
        }

        private bool HasReachedTerminalForDirection()
        {
            int lastIndex = _cachedWaypoints.Length - 1;

            if (_direction > 0)
            {
                return _currentWaypointIndex >= lastIndex && _segmentProgress >= 0.999f;
            }

            return _currentWaypointIndex <= 0 && _segmentProgress <= 0.001f;
        }

        private void ExecuteAction(
            KinematicPlatformSignalAction action,
            KinematicPlatformSignalTravelMode travelMode = KinematicPlatformSignalTravelMode.OneWay)
        {
            switch (action)
            {
                case KinematicPlatformSignalAction.PlayForward:
                    _direction = 1;
                    _isMoving = true;
                    _returningByPolicy = false;
                    _continueToEndThenStop = false;
                    ApplySignalTravelMode(travelMode);
                    break;
                case KinematicPlatformSignalAction.PlayReverse:
                    _direction = -1;
                    _isMoving = true;
                    _returningByPolicy = false;
                    _continueToEndThenStop = false;
                    ApplySignalTravelMode(travelMode);
                    break;
                case KinematicPlatformSignalAction.ToggleDirection:
                    _direction = -_direction;
                    _isMoving = true;
                    _returningByPolicy = false;
                    _continueToEndThenStop = false;
                    ApplySignalTravelMode(travelMode);
                    break;
                case KinematicPlatformSignalAction.Stop:
                    _isMoving = false;
                    _stopAtTerminal = false;
                    _continueToEndThenStop = false;
                    _returningByPolicy = false;
                    _signalTravelActive = false;
                    break;
                case KinematicPlatformSignalAction.None:
                default:
                    break;
            }

            if (enableDebugLogs && action != KinematicPlatformSignalAction.None)
            {
                Debug.Log(
                    $"[KinematicPlatform] {name}: action={action}, travel={travelMode}, moving={_isMoving}, dir={_direction}",
                    this);
            }
        }

        private void ApplySignalTravelMode(KinematicPlatformSignalTravelMode travelMode)
        {
            if (activationMode != KinematicPlatformActivationMode.SignalDriven)
            {
                _signalTravelActive = false;
                _stopAtTerminal = false;
                return;
            }

            _signalTravelMode = travelMode;
            _signalTravelActive = true;
            _stopAtTerminal = travelMode == KinematicPlatformSignalTravelMode.OneWay;
        }

        private void ApplyEndPolicy(KinematicPlatformEndPolicy policy)
        {
            _stopAtTerminal = false;
            _signalTravelActive = false;

            switch (policy)
            {
                case KinematicPlatformEndPolicy.StopInPlace:
                    _isMoving = false;
                    _continueToEndThenStop = false;
                    _returningByPolicy = false;
                    break;
                case KinematicPlatformEndPolicy.ReturnToStart:
                    BeginReturnToIndex(0);
                    break;
                case KinematicPlatformEndPolicy.ReturnToEnd:
                    BeginReturnToIndex(_cachedWaypoints.Length - 1);
                    break;
                case KinematicPlatformEndPolicy.ContinueToEndThenStop:
                    _continueToEndThenStop = true;
                    _returningByPolicy = false;
                    _isMoving = true;
                    break;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[KinematicPlatform] {name}: endPolicy={policy}", this);
            }
        }

        private void BeginReturnToIndex(int targetIndex)
        {
            if (_cachedWaypoints.Length == 0)
            {
                _isMoving = false;
                return;
            }

            _returnTargetIndex = Mathf.Clamp(targetIndex, 0, _cachedWaypoints.Length - 1);
            _returningByPolicy = true;
            _continueToEndThenStop = false;
            _isMoving = true;
            _segmentProgress = 0f;
        }

        private int FindClosestWaypointIndex(Vector3 worldPosition)
        {
            if (_cachedWaypoints.Length == 0)
            {
                return 0;
            }

            int bestIndex = 0;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _cachedWaypoints.Length; i++)
            {
                float distance = Vector3.SqrMagnitude(_cachedWaypoints[i] - worldPosition);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private float ApplyEasing(float t)
        {
            return easing == KinematicPlatformEasing.SmoothStep
                ? t * t * (3f - 2f * t)
                : t;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawPathGizmo)
            {
                return;
            }

            Vector3[] points = GetEditorWaypointPositions();

            if (points.Length == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);

            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawSphere(points[i], 0.12f);

                if (i < points.Length - 1)
                {
                    Gizmos.DrawLine(points[i], points[i + 1]);
                }
            }

            if (activationMode == KinematicPlatformActivationMode.Cycle
                && loopMode == KinematicPlatformLoopMode.Loop
                && points.Length > 2)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
                Gizmos.DrawLine(points[^1], points[0]);
            }
        }
    }
}
