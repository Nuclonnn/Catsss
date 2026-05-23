using System;
using System.Collections.Generic;
using Catsss.Configs.Charge;
using Catsss.Core.Events;
using Catsss.Core.Services;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>
    /// Серверный реестр испытаний на сцене: старт, завершение, выдача наград всей команде.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class TrialSessionRegistry : NetworkBehaviour
    {
        [SerializeField] private GameplayContentCatalog contentCatalog;
        [SerializeField] private TrialProgressEventChannel trialProgressChannel;

        private readonly HashSet<int> _completedTrialIds = new();
        private readonly HashSet<int> _activeTrialIds = new();
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
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public override void OnDestroy()
        {
            ServiceLocator.Unregister<TrialSessionRegistry>();
            base.OnDestroy();
        }

        public bool IsTrialCompleted(int trialId) => _completedTrialIds.Contains(trialId);

        public bool IsTrialActive(int trialId) => _activeTrialIds.Contains(trialId);

        public void RegisterPylon(TrialPylonStart pylon)
        {
            if (pylon != null && !_registeredPylons.Contains(pylon))
            {
                _registeredPylons.Add(pylon);
            }
        }

        public void UnregisterPylon(TrialPylonStart pylon)
        {
            _registeredPylons.Remove(pylon);
        }

        /// <summary>Вызывается только на сервере из пилона.</summary>
        public bool TryBeginTrial(TrialDefinition trial, NetworkPlayerController initiator)
        {
            if (!IsServer || trial == null || !trial.IsValid)
            {
                return false;
            }

            int trialId = trial.TrialId;

            if (_completedTrialIds.Contains(trialId) || _activeTrialIds.Contains(trialId))
            {
                return false;
            }

            if (initiator == null || !initiator.TryGetComponent(out PlayerChargeController chargeController))
            {
                return false;
            }

            if (!chargeController.TryApplyChargeServer(trial.ChargeTypeId, trialId))
            {
                return false;
            }

            _activeTrialIds.Add(trialId);
            RaiseTrialProgress(trialId, TrialProgressPhase.Started);
            return true;
        }

        /// <summary>Финиш зоны: снять заряд у носителя, выдать перманент всем, закрыть испытание.</summary>
        public void CompleteTrial(TrialDefinition trial, PlayerChargeController finisher)
        {
            if (!IsServer || trial == null || !trial.IsValid)
            {
                return;
            }

            int trialId = trial.TrialId;

            if (_completedTrialIds.Contains(trialId))
            {
                return;
            }

            if (finisher == null || finisher.ActiveTrialId != trialId || finisher.ChargeId == 0)
            {
                return;
            }

            finisher.ClearChargeServer();

            PermanentModifierDefinition reward = trial.CompletionReward;

            if (reward != null)
            {
                ApplyModifierToAllConnectedPlayers(reward);
            }

            _activeTrialIds.Remove(trialId);
            _completedTrialIds.Add(trialId);

            NotifyTrialObjectsCompleted(trialId);
            RaiseTrialProgress(trialId, TrialProgressPhase.Completed);
        }

        private void ApplyModifierToAllConnectedPlayers(PermanentModifierDefinition definition)
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

                if (client.PlayerObject.TryGetComponent(out PlayerPermanentModifiers modifiers))
                {
                    modifiers.ApplyModifierServer(definition);
                }
            }
        }

        /// <summary>Сброс активного испытания без награды (внутренние отмены, не штраф).</summary>
        public void CancelActiveTrial(int trialId)
        {
            if (!IsServer || !_activeTrialIds.Remove(trialId))
            {
                return;
            }

            ForPylonsWithTrialId(trialId, static pylon => pylon.ResetTrialActiveServer());
            RaiseTrialProgress(trialId, TrialProgressPhase.Cancelled);
        }

        /// <summary>
        /// Командный штраф: снять заряды у всех, телепорт на чекпоинт пилона, сброс активного испытания (без кулдауна).
        /// </summary>
        public void ApplyTeamTrialPenalty(int trialId, TrialPenaltyReason reason)
        {
            if (!IsServer || trialId <= 0 || !_activeTrialIds.Contains(trialId))
            {
                return;
            }

            _activeTrialIds.Remove(trialId);

            if (!TryResolvePenaltySpawn(trialId, out Vector3 spawnPosition, out Quaternion spawnRotation))
            {
                Debug.LogWarning($"[TrialSessionRegistry] Нет penalty spawn для trialId={trialId}.", this);
                spawnPosition = Vector3.zero;
                spawnRotation = Quaternion.identity;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                foreach (NetworkClient client in networkManager.ConnectedClientsList)
                {
                    if (client.PlayerObject == null)
                    {
                        continue;
                    }

                    if (client.PlayerObject.TryGetComponent(out PlayerChargeController chargeController))
                    {
                        chargeController.ForceClearChargeServer();
                    }

                    if (client.PlayerObject.TryGetComponent(out NetworkPlayerController playerController))
                    {
                        playerController.TeleportFromServerClientRpc(spawnPosition, spawnRotation);
                    }
                }
            }

            ForPylonsWithTrialId(trialId, static pylon => pylon.ResetTrialActiveServer());
            RaiseTrialProgress(trialId, TrialProgressPhase.Cancelled);

            Debug.Log(
                $"[TrialSessionRegistry] Team penalty trialId={trialId} reason={reason} spawn={spawnPosition}",
                this);
        }

        private bool TryResolvePenaltySpawn(int trialId, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            TrialPylonStart resolvedPylon = null;

            ForPylonsWithTrialId(trialId, pylon => resolvedPylon = pylon);

            if (resolvedPylon == null)
            {
                return false;
            }

            Transform spawn = resolvedPylon.PenaltyRespawnPoint;
            position = spawn.position;
            rotation = spawn.rotation;
            return true;
        }

        private void RaiseTrialProgress(int trialId, TrialProgressPhase phase)
        {
            trialProgressChannel?.Invoke(new TrialProgressEvent(trialId, phase));
        }

        private void NotifyTrialObjectsCompleted(int trialId)
        {
            ForPylonsWithTrialId(trialId, static pylon => pylon.MarkCompletedServer());
        }

        private void ForPylonsWithTrialId(int trialId, Action<TrialPylonStart> action)
        {
            for (int i = _registeredPylons.Count - 1; i >= 0; i--)
            {
                TrialPylonStart pylon = _registeredPylons[i];

                if (pylon == null)
                {
                    _registeredPylons.RemoveAt(i);
                    continue;
                }

                if (pylon.TrialId == trialId)
                {
                    action(pylon);
                }
            }
        }

        /// <summary>
        /// После disconnect/reconnect сбрасываем зависшие active trials и заряды у оставшихся игроков.
        /// </summary>
        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer || _activeTrialIds.Count == 0)
            {
                return;
            }

            int[] activeTrials = new int[_activeTrialIds.Count];
            _activeTrialIds.CopyTo(activeTrials);

            Debug.Log(
                $"[TrialSessionRegistry] Client {clientId} disconnected — сброс активных trials: {string.Join(", ", activeTrials)}",
                this);

            foreach (int trialId in activeTrials)
            {
                CancelActiveTrial(trialId);
            }

            ClearAllPlayerChargesServer();
        }

        private void ClearAllPlayerChargesServer()
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

                if (client.PlayerObject.TryGetComponent(out PlayerChargeController chargeController))
                {
                    chargeController.ForceClearChargeServer();
                }
            }
        }
    }
}
