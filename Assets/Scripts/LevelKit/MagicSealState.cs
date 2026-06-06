namespace Catsss.LevelKit
{
    /// <summary>Сетевое состояние универсальной печати уровня.</summary>
    public enum MagicSealState : byte
    {
        Idle = 0,
        Pressed = 1,
        Locked = 2,
        Disabled = 3,
    }
}
