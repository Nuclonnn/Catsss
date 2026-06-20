using System;
using Catsss.Core.Events;
using Catsss.Trials;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Одна реакция мыши на фазу Trial: trialId + phase → MouseRoute.</summary>
    [Serializable]
    public sealed class MouseTrialReactionBinding
    {
        [SerializeField, Min(1)] private int trialId = 1;
        [SerializeField] private TrialProgressPhase phase = TrialProgressPhase.Started;
        [SerializeField] private MouseRoute route;

        [Header("Playback")]
        [SerializeField] private bool playOncePerSession = true;
        [SerializeField] private bool teleportToRouteStart = true;
        [Tooltip("Не запускать, если мышь уже идёт по другому маршруту.")]
        [SerializeField] private bool blockWhileRouteActive = true;
        [Tooltip("Игнорировать Block While Route Active — полезно для Completed поверх Started-сценки.")]
        [SerializeField] private bool interruptActiveRoute;
        [Tooltip("Запускать только если мышь сейчас Hidden (не видна на уровне).")]
        [SerializeField] private bool requireMouseHidden;

        [Header("Events")]
        [SerializeField] private EmptyEventChannel onStartedChannel;
        [SerializeField] private EmptyEventChannel onFinishedChannel;

        public int TrialId => trialId;
        public TrialProgressPhase Phase => phase;
        public MouseRoute Route => route;
        public bool PlayOncePerSession => playOncePerSession;
        public bool TeleportToRouteStart => teleportToRouteStart;
        public bool BlockWhileRouteActive => blockWhileRouteActive;
        public bool InterruptActiveRoute => interruptActiveRoute;
        public bool RequireMouseHidden => requireMouseHidden;
        public EmptyEventChannel OnStartedChannel => onStartedChannel;
        public EmptyEventChannel OnFinishedChannel => onFinishedChannel;

        public bool Matches(in TrialProgressEvent progressEvent)
        {
            return progressEvent.TrialId == trialId && progressEvent.Phase == phase;
        }
    }
}
