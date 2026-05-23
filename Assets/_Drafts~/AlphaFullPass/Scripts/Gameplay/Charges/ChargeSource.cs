using Catsss.Configs;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges
{
    [RequireComponent(typeof(Collider))]
    public sealed class ChargeSource : NetworkBehaviour
    {
        [SerializeField] private ChargeType chargeType;
        [SerializeField] private Transform respawnPoint;
        [SerializeField, Min(0f)] private float reuseDelay = 0.5f;

        private float _nextUseTime;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || Time.time < _nextUseTime)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder == null || chargeType == null)
            {
                return;
            }

            chargeHolder.SetRespawnPoint(respawnPoint != null ? respawnPoint : transform);
            chargeHolder.GrantChargeServer(chargeType.Id);
            _nextUseTime = Time.time + reuseDelay;
        }
    }
}
