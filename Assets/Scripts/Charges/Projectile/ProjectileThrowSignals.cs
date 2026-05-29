using System;

namespace Catsss.Charges.Projectile
{
    /// <summary>
    /// Статические сигналы метания заряда — подписка UI/VFX/аудио без связи логики с визуалом.
    /// Вызываются из геймплей-кода по мере появления событий (4.1+).
    /// </summary>
    public static class ProjectileThrowSignals
    {
        /// <summary>Оставшиеся попытки броска на trial изменились (remaining, max).</summary>
        public static event Action<int, int, int> ThrowAttemptsChanged;

        /// <summary>Владелец вошёл/вышел из Aim (для подсказок, иконки напарника на краю экрана).</summary>
        public static event Action<ulong, bool> AimModeChanged;

        /// <summary>Снаряд в полёте к clientId (0 = сброс). Для telegraph на ловце и звука нарастания.</summary>
        public static event Action<ulong, ulong> IncomingProjectileTargetChanged;

        public static void RaiseThrowAttemptsChanged(int trialId, int remaining, int max) =>
            ThrowAttemptsChanged?.Invoke(trialId, remaining, max);

        public static void RaiseAimModeChanged(ulong ownerClientId, bool isAiming) =>
            AimModeChanged?.Invoke(ownerClientId, isAiming);

        public static void RaiseIncomingProjectileTargetChanged(ulong catcherClientId, ulong throwerClientId) =>
            IncomingProjectileTargetChanged?.Invoke(catcherClientId, throwerClientId);
    }
}
