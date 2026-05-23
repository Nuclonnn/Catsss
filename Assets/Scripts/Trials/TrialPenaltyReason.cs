namespace Catsss.Trials
{
    /// <summary>Причина командного штрафа. Расширяется при добавлении VFX/звука/локализации.</summary>
    public enum TrialPenaltyReason
    {
        None = 0,
        LeftTrialBounds = 1,
        ChargeTimerExpired = 2,
    }
}
