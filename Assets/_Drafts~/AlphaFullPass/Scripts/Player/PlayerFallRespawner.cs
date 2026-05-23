using Catsss.Configs;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(PlayerChargeHolder))]
    public sealed class PlayerFallRespawner : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;

        private PlayerChargeHolder _chargeHolder;

        private void Awake()
        {
            _chargeHolder = GetComponent<PlayerChargeHolder>();
        }

        private void Update()
        {
            if (!IsOwner || gameConfig == null || _chargeHolder == null || !_chargeHolder.HasCharge)
            {
                return;
            }

            if (transform.position.y < gameConfig.PlayerMovement.fallRespawnY)
            {
                _chargeHolder.RequestClearCharge(true);
            }
        }
    }
}
