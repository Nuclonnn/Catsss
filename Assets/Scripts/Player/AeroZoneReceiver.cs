using System.Collections.Generic;
using Catsss.Configs.Charge;
using Catsss.LevelKit;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>
    /// Суммирует влияние активных <see cref="AeroZone"/> на owner-клиенте.
    /// Heavy-заряд игнорирует ветер; Air-заряд усиливает поток.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkPlayerController))]
    public sealed class AeroZoneReceiver : NetworkBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool drawWindDebug;
        [SerializeField] private Color windDebugColor = new(0.3f, 0.9f, 1f, 0.95f);

        private readonly HashSet<AeroZone> _activeZones = new();
        private PlayerChargeController _chargeController;

        public void RegisterZone(AeroZone zone)
        {
            if (zone == null)
            {
                return;
            }

            _activeZones.Add(zone);
        }

        public void UnregisterZone(AeroZone zone)
        {
            if (zone == null)
            {
                return;
            }

            _activeZones.Remove(zone);
        }

        private void Awake()
        {
            _chargeController = GetComponent<PlayerChargeController>();
        }

        /// <summary>
        /// Собирает суммарное влияние ветра для текущего кадра физики.
        /// </summary>
        public AeroZoneWindInfluence SampleWindInfluence(Vector3 worldVelocity)
        {
            AeroZoneWindInfluence influence = default;

            if (!IsOwner || _activeZones.Count == 0)
            {
                return influence;
            }

            float chargeMultiplier = GetWindChargeMultiplier();

            if (chargeMultiplier <= 0f)
            {
                return influence;
            }

            Vector3 combinedTarget = Vector3.zero;
            float maxApproach = 0f;
            float equilibriumAccel = 0f;
            bool hasEquilibrium = false;

            foreach (AeroZone zone in _activeZones)
            {
                if (zone == null || !zone.IsZoneActive)
                {
                    continue;
                }

                maxApproach = Mathf.Max(maxApproach, zone.ApproachAcceleration);

                if (zone.WindMode == AeroZoneWindMode.VerticalEquilibrium)
                {
                    hasEquilibrium = true;
                    combinedTarget += zone.GetHorizontalTargetVelocity(chargeMultiplier);
                    equilibriumAccel += zone.GetEquilibriumVerticalAcceleration(
                        transform.position.y,
                        worldVelocity.y,
                        chargeMultiplier);
                }
                else
                {
                    combinedTarget += zone.GetTargetVelocity(chargeMultiplier);
                }
            }

            influence.HasInfluence = combinedTarget.sqrMagnitude > 0.0001f || hasEquilibrium;
            influence.TargetVelocity = combinedTarget;
            influence.ApproachAcceleration = maxApproach > 0f ? maxApproach : 18f;
            influence.HasEquilibrium = hasEquilibrium;
            influence.EquilibriumVerticalAcceleration = equilibriumAccel;

            if (drawWindDebug && influence.HasInfluence)
            {
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                Debug.DrawRay(origin, influence.TargetVelocity * 0.25f, windDebugColor);
            }

            return influence;
        }

        private float GetWindChargeMultiplier()
        {
            ChargeTypeDefinition charge = _chargeController != null ? _chargeController.ActiveDefinition : null;

            if (charge != null && charge.IsHeavy)
            {
                return 0f;
            }

            if (charge != null && charge.IsAir)
            {
                return charge.AirWindSpeedMultiplier;
            }

            return 1f;
        }
    }

    /// <summary>Результат суммирования зон для одного FixedUpdate.</summary>
    public struct AeroZoneWindInfluence
    {
        public bool HasInfluence;
        public Vector3 TargetVelocity;
        public float ApproachAcceleration;
        public bool HasEquilibrium;
        public float EquilibriumVerticalAcceleration;
    }
}
