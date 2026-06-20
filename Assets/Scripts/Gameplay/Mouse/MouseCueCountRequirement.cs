namespace Catsss.Gameplay.Mouse
{
    /// <summary>Сколько игроков должно быть в зоне cue-триггера.</summary>
    public enum MouseCueCountRequirement : byte
    {
        AnyPlayerInZone = 0,
        MinimumPlayers = 1,
        AllConnectedPlayers = 2,
    }
}
