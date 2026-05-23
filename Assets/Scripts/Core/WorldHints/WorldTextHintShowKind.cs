namespace Catsss.Core.WorldHints
{
    public enum WorldTextHintShowKind : byte
    {
        Manual = 0,
        OnEnable = 1,
        TrialProgress = 2,
        EmptyEvent = 3,
        /// <summary>Когда у локального игрока CanDash стал true (репликация NGO). Надёжнее TrialProgress на клиенте.</summary>
        PlayerCanDashUnlocked = 4,
    }
}
