namespace Catsss.LevelKit
{
    /// <summary>Кто может удерживать Resonance-платформу в движении.</summary>
    public enum KinematicPlatformResonanceRequirement : byte
    {
        AnyPlayer = 0,
        AnyActiveCharge = 1,
        HeavyCharge = 2,
    }
}
