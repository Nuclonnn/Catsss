using System.Collections.Generic;
using Catsss.Configs.Charge;
using Catsss.Core.Events;
using Catsss.Core.Services;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>
    /// Серверный реестр испытаний на сцене: старт, завершение, выдача наград всей команде.
    /// Разбит на partial-файлы: Progress, Throws, Modifiers, SessionEnd.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed partial class TrialSessionRegistry : NetworkBehaviour
    {
        [SerializeField] private GameplayContentCatalog contentCatalog;
        [SerializeField] private TrialProgressEventChannel trialProgressChannel;
        [SerializeField] private bool enableThrowAttemptsDebugLogs;

        private readonly HashSet<int> _completedTrialIds = new();
        private readonly HashSet<int> _activeTrialIds = new();
        private readonly HashSet<byte> _sessionEarnedModifierIds = new();
        private readonly Dictionary<int, int> _attemptsLeftByTrial = new();
        private readonly List<TrialPylonStart> _registeredPylons = new();

        public GameplayContentCatalog ContentCatalog => contentCatalog;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        public override void OnDestroy()
        {
            ServiceLocator.Unregister<TrialSessionRegistry>();
            base.OnDestroy();
        }

        private void RaiseTrialProgress(int trialId, TrialProgressPhase phase)
        {
            trialProgressChannel?.Invoke(new TrialProgressEvent(trialId, phase));
        }
    }
}
