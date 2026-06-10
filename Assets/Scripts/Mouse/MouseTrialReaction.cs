using System.Collections.Generic;
using Catsss.Core.Events;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>
    /// Слушает TrialProgressEventChannel и запускает сценические маршруты мыши
    /// при Started / Completed / Cancelled конкретного Trial.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseTrialReaction : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private TrialProgressEventChannel trialProgressChannel;

        [Header("Target")]
        [SerializeField] private MouseBrain mouse;

        [Header("Reactions")]
        [SerializeField] private MouseTrialReactionBinding[] bindings = System.Array.Empty<MouseTrialReactionBinding>();

        [Header("Debug")]
        [SerializeField] private bool logReactions;

        private readonly HashSet<int> _consumedBindingIndices = new();

        private void OnEnable()
        {
            if (trialProgressChannel != null)
            {
                trialProgressChannel.Raised += OnTrialProgressRaised;
            }
        }

        private void OnDisable()
        {
            if (trialProgressChannel != null)
            {
                trialProgressChannel.Raised -= OnTrialProgressRaised;
            }
        }

        private void OnTrialProgressRaised(TrialProgressEvent progressEvent)
        {
            if (!IsServerAuthority())
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                MouseTrialReactionBinding binding = bindings[i];

                if (binding == null || !binding.Matches(in progressEvent))
                {
                    continue;
                }

                TryPlayBindingServer(i, binding, in progressEvent);
            }
        }

        private void TryPlayBindingServer(int bindingIndex, MouseTrialReactionBinding binding, in TrialProgressEvent progressEvent)
        {
            if (mouse == null)
            {
                Debug.LogWarning($"[MouseTrialReaction] {name}: не назначена Mouse.", this);
                return;
            }

            if (binding.Route == null)
            {
                Debug.LogWarning(
                    $"[MouseTrialReaction] {name}: binding #{bindingIndex} (trial {binding.TrialId}, {binding.Phase}) без Route.",
                    this);
                return;
            }

            if (!binding.Route.IsValid(out string error))
            {
                Debug.LogWarning(
                    $"[MouseTrialReaction] {name}: маршрут '{binding.Route.name}' невалиден: {error}",
                    binding.Route);
                return;
            }

            if (binding.PlayOncePerSession && _consumedBindingIndices.Contains(bindingIndex))
            {
                return;
            }

            if (binding.RequireMouseHidden && mouse.PresenceMode != MousePresenceMode.Hidden)
            {
                return;
            }

            bool routeActive = mouse.IsFollowingRoute;

            if (routeActive && binding.BlockWhileRouteActive && !binding.InterruptActiveRoute)
            {
                return;
            }

            if (binding.PlayOncePerSession)
            {
                _consumedBindingIndices.Add(bindingIndex);
            }

            if (logReactions)
            {
                Debug.Log(
                    $"[MouseTrialReaction] {name}: trial {progressEvent.TrialId} {progressEvent.Phase} -> {binding.Route.name}",
                    this);
            }

            mouse.PlayRouteServer(
                binding.Route,
                binding.TeleportToRouteStart,
                binding.OnStartedChannel,
                binding.OnFinishedChannel);
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }
    }
}
