using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(NetworkPlayerController))]
    public sealed class NetworkPlayerEventsRelay : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayerController controller;

        private void Reset()
        {
            controller = GetComponent<NetworkPlayerController>();
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<NetworkPlayerController>();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (controller == null)
            {
                return;
            }

            controller.Jumped += HandleJumped;
            controller.Dashed += HandleDashed;
        }

        public override void OnNetworkDespawn()
        {
            if (controller == null)
            {
                return;
            }

            controller.Jumped -= HandleJumped;
            controller.Dashed -= HandleDashed;
        }

        private void HandleJumped()
        {
            if (IsOwner)
            {
                JumpServerRpc();
            }
        }

        private void HandleDashed()
        {
            if (IsOwner)
            {
                DashServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void JumpServerRpc()
        {
            JumpClientRpc();
        }

        [Rpc(SendTo.Server)]
        private void DashServerRpc()
        {
            DashClientRpc();
        }

        [Rpc(SendTo.NotOwner)]
        private void JumpClientRpc()
        {
            // Stage 2 only confirms the network event path. Visual subscribers come later.
        }

        [Rpc(SendTo.NotOwner)]
        private void DashClientRpc()
        {
            // Stage 2 only confirms the network event path. Visual subscribers come later.
        }
    }
}
