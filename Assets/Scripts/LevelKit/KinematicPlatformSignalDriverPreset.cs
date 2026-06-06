namespace Catsss.LevelKit
{
    /// <summary>Быстрая начальная настройка SignalDriver. Custom — полностью ручная логика.</summary>
    public enum KinematicPlatformSignalDriverPreset : byte
    {
        Custom = 0,
        /// <summary>Toggle: Press Forward, Release Reverse (мост туда-обратно).</summary>
        ToggleBridge = 1,
        /// <summary>Momentary trigger: Press Forward, Release Reverse (лифт/дверь).</summary>
        HoldElevator = 2,
        /// <summary>OneShot: один проход вперёд до конца.</summary>
        OneShotGate = 3,
        /// <summary>OneShot: старт и Yoyo по path до Stop/Toggle.</summary>
        OneShotStartYoyo = 4,
        /// <summary>Toggle: каждое нажатие меняет направление (Yoyo по path).</summary>
        ToggleDirection = 5,
    }
}
