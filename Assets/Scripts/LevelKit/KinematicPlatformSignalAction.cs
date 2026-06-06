namespace Catsss.LevelKit
{
    /// <summary>Команда платформе от signal driver или resonance.</summary>
    public enum KinematicPlatformSignalAction : byte
    {
        None = 0,
        PlayForward = 1,
        PlayReverse = 2,
        ToggleDirection = 3,
        Stop = 4,
    }
}
