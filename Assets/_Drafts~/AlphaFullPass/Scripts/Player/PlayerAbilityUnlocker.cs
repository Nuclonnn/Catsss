using Catsss.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    public sealed class PlayerAbilityUnlocker : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private StringEventChannel abilityUnlockedChannel;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        public void UnlockForEveryone(string abilityName)
        {
            if (!IsServer)
            {
                return;
            }

            UnlockAbilityClientRpc(abilityName);
        }

        [ClientRpc]
        private void UnlockAbilityClientRpc(string abilityName)
        {
            if (playerController != null)
            {
                playerController.UnlockAbility(abilityName);
            }

            abilityUnlockedChannel?.Invoke(abilityName);
        }
    }
}
