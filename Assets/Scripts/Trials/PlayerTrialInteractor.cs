using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Владелец запрашивает старт испытания на сервере (RPC на объекте игрока).</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTrialInteractor : NetworkBehaviour
    {
        public void RequestStartTrial(TrialPylonStart pylon)
        {
            if (!IsOwner || pylon == null)
            {
                return;
            }

            NetworkObject pylonNetworkObject = pylon.NetworkObject;

            if (pylonNetworkObject == null || !pylonNetworkObject.IsSpawned)
            {
                Debug.LogWarning(
                    "[PlayerTrialInteractor] Пилон ещё не синхронизирован по сети (NetworkObject не spawned). Подожди или перезайди в сессию.",
                    pylon);
                return;
            }

            StartTrialServerRpc(pylonNetworkObject.NetworkObjectId);
        }

        [Rpc(SendTo.Server)]
        private void StartTrialServerRpc(ulong pylonNetworkObjectId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null
                || !networkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    pylonNetworkObjectId,
                    out NetworkObject pylonObject))
            {
                return;
            }

            if (!pylonObject.TryGetComponent(out TrialPylonStart pylon))
            {
                return;
            }

            if (!TryGetComponent(out NetworkPlayerController controller))
            {
                return;
            }

            pylon.HandleInteractServer(controller);
        }
    }
}
