using Catsss.Core.Events;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Catsss.Gameplay.PuzzleBlocks
{
    [RequireComponent(typeof(Collider))]
    public sealed class Pylon : NetworkBehaviour
    {
        [SerializeField] private int pylonId = 1;
        [SerializeField] private IntEventChannel pylonActivatedChannel;
        [SerializeField] private UnityEvent activated;

        private readonly NetworkVariable<bool> _isActivated = new();

        public int PylonId => pylonId;
        public bool IsActivated => _isActivated.Value;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        public override void OnNetworkSpawn()
        {
            _isActivated.OnValueChanged += HandleActivationChanged;
        }

        public override void OnNetworkDespawn()
        {
            _isActivated.OnValueChanged -= HandleActivationChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _isActivated.Value)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder == null || !chargeHolder.HasCharge)
            {
                return;
            }

            chargeHolder.ClearChargeServer(false);
            _isActivated.Value = true;
            pylonActivatedChannel?.Invoke(pylonId);
        }

        private void HandleActivationChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                activated?.Invoke();
            }
        }
    }
}
