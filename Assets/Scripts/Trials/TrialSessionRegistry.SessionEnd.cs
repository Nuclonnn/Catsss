using System.Collections.Generic;
using Catsss.Gameplay.Charges.Projectile;
using Catsss.Menu;
using Catsss.Menu.Flow;
using Catsss.Network;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Завершение NGO-сессии и сброс при disconnect клиента.</summary>
    public sealed partial class TrialSessionRegistry
    {
        /// <summary>Хост завершает сессию: клиенты получают reason до transport disconnect.</summary>
        public void NotifyRemoteClientsSessionEndingServer(MenuReturnReason reason)
        {
            if (!IsServer)
            {
                return;
            }

            NotifySessionEndingClientRpc((byte)reason);
        }

        [ClientRpc]
        private void NotifySessionEndingClientRpc(byte reasonByte)
        {
            MenuReturnReason reason = (MenuReturnReason)reasonByte;
            SessionEndSignal.MarkExpectedRemoteEnd(reason);

            if (IsHost)
            {
                return;
            }

            IAppFlowCommands flow = AppFlow.EnsureExists();

            if (flow.IsInGameFlow && !flow.IsReturningFlow)
            {
                flow.RequestReturnToMainMenu(reason, stopNetwork: true);
            }
        }

        /// <summary>
        /// Клиент ушёл: сессия хоста продолжается; сбрасываем только зависшие active trials.
        /// </summary>
        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            int connectedCount = networkManager?.ConnectedClientsList?.Count ?? 0;

            if (_activeTrialIds.Count == 0)
            {
                Debug.Log(
                    $"[TrialSessionRegistry] Client {clientId} disconnected. Session continues ({connectedCount} client(s)).",
                    this);
                return;
            }

            int[] activeTrials = new int[_activeTrialIds.Count];
            _activeTrialIds.CopyTo(activeTrials);

            HashSet<ulong> restoredThrowerIds = ChargeProjectile.AbortAllInFlightOnDisconnectServer(clientId);

            Debug.Log(
                $"[TrialSessionRegistry] Client {clientId} disconnected — сброс активных trials: {string.Join(", ", activeTrials)}",
                this);

            foreach (int trialId in activeTrials)
            {
                CancelActiveTrial(trialId);
            }

            ClearAllPlayerChargesServer(restoredThrowerIds);
        }

        private void ClearAllPlayerChargesServer(HashSet<ulong> skipClientIds = null)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    continue;
                }

                if (skipClientIds != null && skipClientIds.Contains(client.ClientId))
                {
                    continue;
                }

                if (client.PlayerObject.TryGetComponent(out PlayerChargeController chargeController))
                {
                    chargeController.ForceClearChargeServer();
                }
            }
        }
    }
}
