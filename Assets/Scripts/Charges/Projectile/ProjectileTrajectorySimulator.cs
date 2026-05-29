using System.Collections.Generic;
using Catsss.Configs;
using UnityEngine;

namespace Catsss.Charges.Projectile
{
    /// <summary>
    /// Общая серверная/клиентская симуляция полёта снаряда (прямой + homing) для предпросмотра и геймплея.
    /// </summary>
    public static class ProjectileTrajectorySimulator
    {
        private static readonly List<Vector3> ScratchPoints = new(64);

        /// <summary>
        /// Симулирует траекторию. Возвращает переиспользуемый буфер — читать только в том же кадре.
        /// </summary>
        public static IReadOnlyList<Vector3> Simulate(
            Vector3 origin,
            Vector3 direction,
            Vector3? targetWorldPosition,
            ProjectileSettings settings)
        {
            ScratchPoints.Clear();

            if (settings == null)
            {
                ScratchPoints.Add(origin);
                return ScratchPoints;
            }

            int steps = Mathf.Max(4, settings.trajectorySimulationSteps);
            float stepDt = Mathf.Max(0.01f, settings.trajectorySimulationStep);
            float speed = settings.throwSpeed;
            float turnSpeedDeg = settings.homingTurnSpeedDegPerSec;

            Vector3 position = origin;
            Vector3 flightDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            ScratchPoints.Add(position);

            for (int i = 0; i < steps; i++)
            {
                if (targetWorldPosition.HasValue)
                {
                    flightDirection = ApplyHomingStep(
                        flightDirection,
                        position,
                        targetWorldPosition.Value,
                        turnSpeedDeg,
                        stepDt);
                }

                position += flightDirection * (speed * stepDt);
                ScratchPoints.Add(position);
            }

            return ScratchPoints;
        }

        /// <summary>Один шаг homing — тот же алгоритм, что у <see cref="ChargeProjectile"/>.</summary>
        public static Vector3 ApplyHomingStep(
            Vector3 direction,
            Vector3 position,
            Vector3 targetWorldPosition,
            float turnSpeedDegPerSec,
            float deltaTime)
        {
            Vector3 toTarget = targetWorldPosition - position;

            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return direction;
            }

            Vector3 desiredDirection = toTarget.normalized;
            float maxRadians = turnSpeedDegPerSec * Mathf.Deg2Rad * deltaTime;
            return Vector3.RotateTowards(direction, desiredDirection, maxRadians, 0f).normalized;
        }
    }
}
