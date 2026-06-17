using System;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(NetworkPlayerController))]
    public sealed class NetworkPlayerEventsRelay : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayerController controller;

        /// <summary>Срабатывает на удалённых клиентах после RPC прыжка владельца.</summary>
        public event Action RemoteJumped;

        /// <summary>Срабатывает на удалённых клиентах после RPC рывка владельца.</summary>
        public event Action RemoteDashed;

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
            RemoteJumped?.Invoke();
        }

        [Rpc(SendTo.NotOwner)]
        private void DashClientRpc()
        {
            RemoteDashed?.Invoke();
        }
    }
}
