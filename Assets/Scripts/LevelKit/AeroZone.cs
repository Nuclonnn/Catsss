using System;
using System.Collections.Generic;
using Catsss.Player;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Аэрозона (ветер): trigger-box задаёт направление и силу потока.
    /// Gameplay только на owner-клиенте игрока через <see cref="AeroZoneReceiver"/>.
    /// Состояние вкл/выкл синхронизируется signal driver'ом для визуала и логики.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AeroZone : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool zoneStartsEnabled = true;

        [Header("Wind")]
        [SerializeField] private AeroZoneWindMode windMode = AeroZoneWindMode.TargetVelocity;
        [SerializeField] private Vector3 windDirection = Vector3.up;
        [SerializeField] private bool useLocalDirection = true;
        [Min(0f)] [SerializeField] private float targetSpeed = 6f;
        [Min(0.1f)] [SerializeField] private float approachAcceleration = 18f;

        [Header("Vertical Equilibrium (Updraft)")]
        [Tooltip("0 = низ box-коллайдера, 1 = верх. Игрок «оседает» на этой высоте.")]
        [SerializeField, Range(0f, 1f)] private float equilibriumNormalizedHeight = 0.65f;
        [Min(0.1f)] [SerializeField] private float equilibriumSpring = 24f;
        [Min(0f)] [SerializeField] private float equilibriumDamping = 6f;

        [Header("Debug")]
        [SerializeField] private bool drawZoneGizmo = true;
        [SerializeField] private Color gizmoColor = new(0.35f, 0.85f, 1f, 0.35f);
        [SerializeField] private bool drawWindArrow = true;

        private readonly HashSet<AeroZoneReceiver> _occupants = new();
        private bool _isZoneActive;

        public bool IsZoneActive => _isZoneActive;
        public AeroZoneWindMode WindMode => windMode;
        public float TargetSpeed => targetSpeed;
        public float ApproachAcceleration => approachAcceleration;

        public event Action<bool> ZoneActiveChanged;

        private void Awake()
        {
            _isZoneActive = zoneStartsEnabled;
        }

        private void Start()
        {
            ZoneActiveChanged?.Invoke(_isZoneActive);
        }

        /// <summary>Сервер: runtime-состояние от signal driver.</summary>
        public void SetRuntimeStateServer(bool zoneActive)
        {
            ApplyZoneActiveState(zoneActive);
        }

        /// <summary>Клиенты: синхронизация визуала и локальной логики ветра.</summary>
        public void ApplyVisualStateClient(bool zoneActive)
        {
            ApplyZoneActiveState(zoneActive);
        }

        private void ApplyZoneActiveState(bool zoneActive)
        {
            if (_isZoneActive == zoneActive)
            {
                return;
            }

            _isZoneActive = zoneActive;

            if (!_isZoneActive)
            {
                ClearOccupants();
            }

            ZoneActiveChanged?.Invoke(_isZoneActive);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isZoneActive || !TryResolveOwnerReceiver(other, out AeroZoneReceiver receiver))
            {
                return;
            }

            _occupants.Add(receiver);
            receiver.RegisterZone(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryResolveOwnerReceiver(other, out AeroZoneReceiver receiver))
            {
                return;
            }

            _occupants.Remove(receiver);
            receiver.UnregisterZone(this);
        }

        /// <summary>Мировое направление потока (единичный вектор).</summary>
        public Vector3 GetWorldWindDirection()
        {
            Vector3 direction = windDirection.sqrMagnitude > 0.0001f ? windDirection : Vector3.up;
            direction.Normalize();

            if (useLocalDirection)
            {
                direction = transform.TransformDirection(direction);
                direction.Normalize();
            }

            return direction;
        }

        /// <summary>Целевая скорость потока с учётом множителя заряда.</summary>
        public Vector3 GetTargetVelocity(float chargeMultiplier)
        {
            if (chargeMultiplier <= 0f || targetSpeed <= 0f)
            {
                return Vector3.zero;
            }

            return GetWorldWindDirection() * (targetSpeed * chargeMultiplier);
        }

        /// <summary>Горизонтальная часть целевой скорости (для режима equilibrium).</summary>
        public Vector3 GetHorizontalTargetVelocity(float chargeMultiplier)
        {
            Vector3 target = GetTargetVelocity(chargeMultiplier);
            target.y = 0f;
            return target;
        }

        /// <summary>Вертикальное ускорение пружины к линии равновесия (м/с²).</summary>
        public float GetEquilibriumVerticalAcceleration(float playerWorldY, float verticalVelocity, float chargeMultiplier)
        {
            if (chargeMultiplier <= 0f || windMode != AeroZoneWindMode.VerticalEquilibrium)
            {
                return 0f;
            }

            float equilibriumY = GetEquilibriumWorldY();
            float displacement = equilibriumY - playerWorldY;
            return displacement * equilibriumSpring * chargeMultiplier - verticalVelocity * equilibriumDamping;
        }

        public float GetEquilibriumWorldY()
        {
            Collider col = GetComponent<Collider>();

            if (col is BoxCollider)
            {
                Bounds bounds = col.bounds;
                return Mathf.Lerp(bounds.min.y, bounds.max.y, equilibriumNormalizedHeight);
            }

            return transform.position.y + equilibriumNormalizedHeight;
        }

        private void ClearOccupants()
        {
            foreach (AeroZoneReceiver receiver in _occupants)
            {
                if (receiver != null)
                {
                    receiver.UnregisterZone(this);
                }
            }

            _occupants.Clear();
        }

        private static bool TryResolveOwnerReceiver(Collider other, out AeroZoneReceiver receiver)
        {
            receiver = null;

            if (other == null)
            {
                return false;
            }

            NetworkPlayerController player = other.GetComponentInParent<NetworkPlayerController>();

            if (player == null || !player.IsOwner)
            {
                return false;
            }

            receiver = player.GetComponent<AeroZoneReceiver>();
            return receiver != null;
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawZoneGizmo)
            {
                return;
            }

            Collider col = GetComponent<Collider>();

            if (col == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }

            if (drawWindArrow)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Vector3 origin = col.bounds.center;
                Vector3 direction = GetWorldWindDirection();
                Gizmos.color = new Color(0.2f, 0.95f, 1f, 0.95f);
                Gizmos.DrawLine(origin, origin + direction * Mathf.Max(1f, targetSpeed * 0.35f));

                if (windMode == AeroZoneWindMode.VerticalEquilibrium && col is BoxCollider)
                {
                    float eqY = GetEquilibriumWorldY();
                    Bounds bounds = col.bounds;
                    Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
                    Vector3 lineStart = new(bounds.min.x, eqY, bounds.center.z);
                    Vector3 lineEnd = new(bounds.max.x, eqY, bounds.center.z);
                    Gizmos.DrawLine(lineStart, lineEnd);
                }
            }
        }
    }
}
