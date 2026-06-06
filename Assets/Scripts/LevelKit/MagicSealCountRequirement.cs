namespace Catsss.LevelKit
{
    /// <summary>Сколько подходящих игроков нужно для удержания trigger-печати.</summary>
    public enum MagicSealCountRequirement : byte
    {
        AnyQualifiedPlayer = 0,
        MinimumQualifiedPlayers = 1,
        AllConnectedPlayers = 2,
    }
}
