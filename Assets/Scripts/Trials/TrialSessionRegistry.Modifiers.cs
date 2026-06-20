using System.Collections;
using Catsss.Configs.Charge;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Перманентные модификаторы сессии и replay для поздно подключившихся клиентов.</summary>
    public sealed partial class TrialSessionRegistry
    {
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

        /// <summary>Новый клиент: выдать все перманентные баффы, заработанные в текущей сессии.</summary>
        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer || _sessionEarnedModifierIds.Count == 0)
            {
                return;
            }

            StartCoroutine(ApplySessionModifiersWhenPlayerReady(clientId));
        }

        private IEnumerator ApplySessionModifiersWhenPlayerReady(ulong clientId)
        {
            const float timeoutSeconds = 15f;
            float elapsed = 0f;

            while (elapsed < timeoutSeconds)
            {
                if (!IsServer)
                {
                    yield break;
                }

                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager == null || !networkManager.IsServer)
                {
                    yield break;
                }

                if (networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
                    && client.PlayerObject != null)
                {
                    ApplySessionModifiersToPlayer(client.PlayerObject);
                    Debug.Log(
                        $"[TrialSessionRegistry] Replayed {_sessionEarnedModifierIds.Count} session modifier(s) to client {clientId}.",
                        this);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning(
                $"[TrialSessionRegistry] Timed out replaying session modifiers to client {clientId}.",
                this);
        }

        private void ApplySessionModifiersToPlayer(NetworkObject playerObject)
        {
            if (!IsServer || playerObject == null
                || !playerObject.TryGetComponent(out PlayerPermanentModifiers modifiers))
            {
                return;
            }

            foreach (byte modifierId in _sessionEarnedModifierIds)
            {
                if (TryResolveModifier(modifierId, out PermanentModifierDefinition definition))
                {
                    modifiers.ApplyModifierServer(definition);
                }
                else
                {
                    Debug.LogWarning(
                        $"[TrialSessionRegistry] Session modifier id={modifierId} not found in catalog.",
                        this);
                }
            }
        }

        private bool TryResolveModifier(byte modifierId, out PermanentModifierDefinition definition)
        {
            definition = null;

            if (contentCatalog?.PermanentModifiers == null)
            {
                return false;
            }

            return contentCatalog.PermanentModifiers.TryGet(modifierId, out definition);
        }
    }
}
