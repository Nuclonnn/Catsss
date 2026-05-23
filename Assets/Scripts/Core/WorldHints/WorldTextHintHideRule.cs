using System;
using Catsss.Core.Events;
using Catsss.Trials;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>Условие скрытия подсказки.</summary>
    [Serializable]
    public struct WorldTextHintHideRule
    {
        [SerializeField] private WorldTextHintHideKind kind;

        [Header("Input")]
        [SerializeField] private WorldTextHintInputKind inputAction;

        [Header("Timeout")]
        [SerializeField, Min(0f)] private float timeoutSeconds;

        [Header("Trial Progress")]
        [SerializeField] private TrialProgressEventChannel trialProgressChannel;
        [SerializeField] private int trialIdFilter;
        [SerializeField] private TrialProgressPhase trialPhase;

        [Header("Empty Event")]
        [SerializeField] private EmptyEventChannel emptyEventChannel;

        public WorldTextHintHideKind Kind => kind;
        public WorldTextHintInputKind InputAction => inputAction;
        public float TimeoutSeconds => timeoutSeconds;
        public TrialProgressEventChannel TrialProgressChannel => trialProgressChannel;
        public int TrialIdFilter => trialIdFilter;
        public TrialProgressPhase TrialPhase => trialPhase;
        public EmptyEventChannel EmptyEventChannel => emptyEventChannel;

        public bool MatchesTrialProgress(in TrialProgressEvent progressEvent)
        {
            if (kind != WorldTextHintHideKind.TrialProgress)
            {
                return false;
            }

            if (trialIdFilter >= 0 && progressEvent.TrialId != trialIdFilter)
            {
                return false;
            }

            return progressEvent.Phase == trialPhase;
        }
    }
}
