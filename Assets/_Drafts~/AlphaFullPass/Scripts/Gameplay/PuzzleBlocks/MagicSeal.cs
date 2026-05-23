using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Catsss.Gameplay.PuzzleBlocks
{
    [RequireComponent(typeof(Collider))]
    public sealed class MagicSeal : NetworkBehaviour
    {
        public enum SealMode
        {
            Standard,
            HeavyWeight,
            Impact
        }

        [SerializeField] private SealMode mode;
        [SerializeField] private bool activateOnce = true;
        [SerializeField] private UnityEvent activated;

        private readonly NetworkVariable<bool> _isActivated = new();

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

        public void RequestStandardActivation(NetworkObject playerObject)
        {
            if (mode != SealMode.Standard || playerObject == null)
            {
                return;
            }

            RequestStandardActivationServerRpc(playerObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestStandardActivationServerRpc(NetworkObjectReference playerReference)
        {
            if (mode != SealMode.Standard || !playerReference.TryGet(out NetworkObject playerObject))
            {
                return;
            }

            TryActivate(playerObject.GetComponent<PlayerController>());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || mode == SealMode.Standard)
            {
                return;
            }

            TryActivate(other.GetComponentInParent<PlayerController>());
        }

        private void TryActivate(PlayerController playerController)
        {
            if (playerController == null || activateOnce && _isActivated.Value)
            {
                return;
            }

            bool isValid = mode switch
            {
                SealMode.Standard => true,
                SealMode.HeavyWeight => playerController.IsHeavy,
                SealMode.Impact => playerController.IsDashing,
                _ => false
            };

            if (!isValid)
            {
                return;
            }

            _isActivated.Value = true;
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
