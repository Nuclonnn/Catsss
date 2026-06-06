using Catsss.LevelKit;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>
    /// Owner-only: сообщает скорость KinematicPlatform и помогает контроллеру ехать «вместе» с платформой.
    /// Перенос через LateUpdate + обнуление мировой скорости в контроллере давали проскальзывание и трение.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingPlatformRider : NetworkBehaviour
    {
        [Header("Detection")]
        [Tooltip("Точка у ног, обычно тот же Transform, что Ground Probe у NetworkPlayerController.")]
        [SerializeField] private Transform feetProbe;

        [SerializeField, Min(0.05f)] private float probeDistance = 0.65f;
        [SerializeField] private LayerMask platformMask = ~0;
        [SerializeField, Min(0.1f)] private float topContactNormalThreshold = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug;

        private Rigidbody _rigidbody;
        private KinematicPlatform _currentPlatform;
        private Vector3 _lastPlatformPosition;
        private bool _hasLastPlatformPosition;

        /// <summary>Игрок стоит на платформе (луч + collision).</summary>
        public bool IsRidingPlatform => _currentPlatform != null;

        /// <summary>Мировая скорость платформы за последний FixedUpdate.</summary>
        public Vector3 PlatformVelocity { get; private set; }

        /// <summary>Горизонтальная часть <see cref="PlatformVelocity"/>.</summary>
        public Vector3 PlatformHorizontalVelocity =>
            new(PlatformVelocity.x, 0f, PlatformVelocity.z);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            ResolveFeetProbe();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            RefreshPlatformReference();
            UpdatePlatformVelocity();
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!IsOwner || collision == null || !HasTopContact(collision))
            {
                return;
            }

            KinematicPlatform platform = ResolvePlatformFromCollision(collision);

            if (platform != null && _currentPlatform != platform)
            {
                BindPlatform(platform);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!IsOwner || collision == null || _currentPlatform == null)
            {
                return;
            }

            KinematicPlatform platform = ResolvePlatformFromCollision(collision);

            if (platform == _currentPlatform && !TryFindPlatformUnderFeet(out _))
            {
                ClearPlatform();
            }
        }

        private void UpdatePlatformVelocity()
        {
            PlatformVelocity = Vector3.zero;

            if (_currentPlatform == null)
            {
                return;
            }

            Vector3 platformPosition = _currentPlatform.transform.position;

            if (!_hasLastPlatformPosition)
            {
                _lastPlatformPosition = platformPosition;
                _hasLastPlatformPosition = true;
                return;
            }

            Vector3 delta = platformPosition - _lastPlatformPosition;
            _lastPlatformPosition = platformPosition;

            // На клиенте ServerNetworkTransform часто обновляет позицию между FixedUpdate.
            if (delta.sqrMagnitude <= 0.000001f)
            {
                delta = _currentPlatform.PlatformDelta;
            }

            if (delta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            PlatformVelocity = delta / Time.fixedDeltaTime;

            if (drawDebug)
            {
                Debug.DrawRay(transform.position, PlatformVelocity, Color.cyan, Time.fixedDeltaTime);
            }
        }

        private void RefreshPlatformReference()
        {
            if (TryFindPlatformUnderFeet(out KinematicPlatform platform))
            {
                if (_currentPlatform != platform)
                {
                    BindPlatform(platform);
                }

                return;
            }

            if (_currentPlatform != null && !IsStandingOnCurrentPlatform())
            {
                ClearPlatform();
            }
        }

        private void BindPlatform(KinematicPlatform platform)
        {
            _currentPlatform = platform;
            _lastPlatformPosition = platform.transform.position;
            _hasLastPlatformPosition = true;
            PlatformVelocity = Vector3.zero;
        }

        private void ClearPlatform()
        {
            _currentPlatform = null;
            _hasLastPlatformPosition = false;
            PlatformVelocity = Vector3.zero;
        }

        private bool TryFindPlatformUnderFeet(out KinematicPlatform platform)
        {
            platform = null;
            Vector3 origin = GetProbeOrigin();
            float distance = Mathf.Max(probeDistance, 0.15f);

            if (!Physics.Raycast(
                    origin + Vector3.up * 0.02f,
                    Vector3.down,
                    out RaycastHit hit,
                    distance,
                    platformMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (drawDebug)
                {
                    Debug.DrawLine(origin, origin + Vector3.down * distance, Color.red, 0.15f);
                }

                return false;
            }

            platform = hit.collider.GetComponentInParent<KinematicPlatform>();

            if (drawDebug)
            {
                Debug.DrawLine(origin, hit.point, platform != null ? Color.green : Color.yellow, 0.15f);
            }

            return platform != null;
        }

        private bool IsStandingOnCurrentPlatform()
        {
            return _currentPlatform != null
                && TryFindPlatformUnderFeet(out KinematicPlatform platform)
                && platform == _currentPlatform;
        }

        private KinematicPlatform ResolvePlatformFromCollision(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out KinematicPlatform platform))
            {
                return platform;
            }

            return collision.gameObject.GetComponentInParent<KinematicPlatform>();
        }

        private bool HasTopContact(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Mathf.Abs(collision.GetContact(i).normal.y) > topContactNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetProbeOrigin()
        {
            return feetProbe != null ? feetProbe.position : transform.position + Vector3.down * 0.45f;
        }

        private void ResolveFeetProbe()
        {
            if (feetProbe != null)
            {
                return;
            }

            Transform found = transform.Find("GroundProbe");

            if (found != null)
            {
                feetProbe = found;
            }
        }
    }
}
