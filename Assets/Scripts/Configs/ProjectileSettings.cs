using System;
using UnityEngine;

namespace Catsss.Configs
{
    /// <summary>
    /// Глобальная физика снаряда и симуляция дуги предпросмотра. Читается с <see cref="GameConfig.Projectile"/>.
    /// Правила заряда (попытки, бонус таймера, кулдаун) — в <see cref="Charge.ChargeTypeDefinition"/>.
    /// </summary>
    [Serializable]
    public sealed class ProjectileSettings
    {
        [Header("Полёт")]
        [Min(0f)]
        [Tooltip("Смещение точки спавна вперёд по направлению броска (м).")]
        public float spawnForwardOffset = 0.75f;

        [Min(0.1f)]
        [Tooltip("Скорость снаряда (м/с), серверная кинематика.")]
        public float throwSpeed = 12f;

        [Min(0f)]
        [Tooltip("Сила доводки homing (град/с).")]
        public float homingTurnSpeedDegPerSec = 110f;

        [Min(0f)]
        [Tooltip("Дистанция полёта по начальному направлению до включения homing (м).")]
        public float homingMinStraightDistance = 1.5f;

        [Min(0.1f)]
        [Tooltip("Макс. время жизни снаряда (с); истечение = промах.")]
        public float maxLifetime = 4f;

        [Header("Направление броска (луч из экрана)")]
        [Range(0f, 1f)]
        [Tooltip("Точка на экране по X (0.5 = центр).")]
        public float aimViewportX = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Точка на экране по Y (0.5 = центр). Выше 0.5 — луч выше, удобнее при камере сверху/за спиной.")]
        public float aimViewportY = 0.58f;

        [Min(1f)]
        [Tooltip("Макс. дистанция луча и запасной точки при промахе (м).")]
        public float aimRayMaxDistance = 80f;

        [Min(0.01f)]
        [Tooltip("Игнор попаданий ближе этой дистанции от origin (отсекает collider кота).")]
        public float aimRayMinDistance = 0.75f;

        [Min(5f)]
        [Tooltip("Дистанция точки прицела вдоль луча от origin при промахе / броске в небо (м).")]
        public float aimRayFallbackDistance = 40f;

        [Range(0f, 1f)]
        [Tooltip("Если луч смотрит вверх (direction.y >= порога), игнорировать попадания в горизонтальный пол.")]
        public float aimSkyAimRayUpThreshold = 0.12f;

        [Tooltip("Слои для raycast прицела (пол, стены, напарник). Собственный кот отсекается кодом.")]
        public LayerMask aimRayLayerMask = ~0;

        [Tooltip("Debug.DrawRay/Line в Scene при Play (только когда вызывается Resolve).")]
        public bool drawAimDirectionDebug;

        [Header("Предпросмотр траектории")]
        [Min(4)]
        public int trajectorySimulationSteps = 40;

        [Min(0.01f)]
        [Tooltip("Шаг dt симуляции дуги (с).")]
        public float trajectorySimulationStep = 0.05f;
    }
}
