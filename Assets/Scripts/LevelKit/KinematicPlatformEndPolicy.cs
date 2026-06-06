namespace Catsss.LevelKit
{
    /// <summary>Что делать, когда удержание сигнала/резонанса закончилось.</summary>
    public enum KinematicPlatformEndPolicy : byte
    {
        StopInPlace = 0,
        ReturnToStart = 1,
        ReturnToEnd = 2,
        ContinueToEndThenStop = 3,
    }
}
