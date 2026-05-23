using System;

namespace Catsss.Trials
{
    [Serializable]
    public readonly struct TrialProgressEvent
    {
        public int TrialId { get; }
        public TrialProgressPhase Phase { get; }

        public TrialProgressEvent(int trialId, TrialProgressPhase phase)
        {
            TrialId = trialId;
            Phase = phase;
        }
    }
}
