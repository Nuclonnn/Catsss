using System;
using Catsss.Core.Events;
using Catsss.Trials;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>Условие показа подсказки. Настраивается в Definition / Trigger без кода.</summary>
    [Serializable]
    public struct WorldTextHintShowRule
    {
        [SerializeField] private WorldTextHintShowKind kind;

        [Header("Trial Progress")]
        [SerializeField] private TrialProgressEventChannel trialProgressChannel;
        [SerializeField] private int trialIdFilter;
        [SerializeField] private TrialProgressPhase trialPhase;

        [Header("Empty Event")]
        [SerializeField] private EmptyEventChannel emptyEventChannel;

        [Header("Player filter (локальный владелец)")]
        [Tooltip("Показывать только если CanDash уже true (например после награды испытания).")]
        [SerializeField] private bool requireLocalPlayerCanDash;
        [Tooltip("Не показывать при старте сцены — только когда событие пришло в runtime.")]
        [SerializeField] private bool requireRuntimeEvent;

        public WorldTextHintShowKind Kind => kind;
        public TrialProgressEventChannel TrialProgressChannel => trialProgressChannel;
        public int TrialIdFilter => trialIdFilter;
        public TrialProgressPhase TrialPhase => trialPhase;
        public EmptyEventChannel EmptyEventChannel => emptyEventChannel;
        public bool RequireLocalPlayerCanDash => requireLocalPlayerCanDash;
        public bool RequireRuntimeEvent => requireRuntimeEvent;

        public bool MatchesTrialProgress(in TrialProgressEvent progressEvent)
        {
            if (kind != WorldTextHintShowKind.TrialProgress)
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
