namespace Catsss.LevelKit
{
    /// <summary>Команда signal driver: вкл/выкл зоны ветра.</summary>
    public enum AeroZoneSignalAction : byte
    {
        None = 0,
        Enable = 1,
        Disable = 2,
        Toggle = 3,
    }
}
