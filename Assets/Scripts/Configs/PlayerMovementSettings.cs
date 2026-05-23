using System;
using UnityEngine;

namespace Catsss.Configs
{
    [Serializable]
    public sealed class PlayerMovementSettings
    {
        [Min(0f)] public float baseSpeed = 6f;
        [Min(0f)] public float acceleration = 35f;
        [Min(0f)] public float rotationSpeed = 720f;
        /// <summary>
        /// Сглаживание вектора движения относительно камеры — убирает тряску меша при плавном вращении орбиты (0 = выкл.).
        /// </summary>
        [Min(0f)] public float cameraRelativeDirectionSmoothTime = 0.04f;
        [Min(0.05f)] public float jumpDuration = 0.45f;
        [Min(0.1f)] public float jumpHeight = 2.2f;
        [Min(0f)] public float gravityMultiplier = 2.5f;
        [Min(0f)] public float jumpCutGravityMultiplier = 2f;
        [Range(0f, 0.3f)] public float coyoteTime = 0.1f;
        [Range(0f, 0.3f)] public float jumpBufferTime = 0.12f;
        [Min(1f)] public float dashSpeedMultiplier = 2.75f;
        [Min(0.01f)] public float dashDuration = 0.18f;
        [Min(0f)] public float dashCooldown = 0.35f;

        /// <summary>
        /// Множитель к <see cref="baseSpeed"/>, если после дэша Shift всё ещё зажат, пока игрок его не отпустит.
        /// </summary>
        [Min(1f)] public float postDashSprintSpeedMultiplier = 1.75f;
    }
}
