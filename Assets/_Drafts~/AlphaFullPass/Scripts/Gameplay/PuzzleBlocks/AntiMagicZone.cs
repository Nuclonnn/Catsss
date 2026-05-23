using Catsss.Gameplay.Charges;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.PuzzleBlocks
{
    [RequireComponent(typeof(Collider))]
    public sealed class AntiMagicZone : NetworkBehaviour
    {
        [SerializeField] private bool blocksProjectiles;
        [SerializeField] private bool teleportChargedPlayers = true;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder != null && chargeHolder.HasCharge)
            {
                chargeHolder.ClearChargeServer(teleportChargedPlayers);
                return;
            }

            if (blocksProjectiles)
            {
                HomingChargeProjectile projectile = other.GetComponentInParent<HomingChargeProjectile>();
                if (projectile != null)
                {
                    projectile.Fizzle(projectile.transform.position);
                }
            }
        }
    }
}
