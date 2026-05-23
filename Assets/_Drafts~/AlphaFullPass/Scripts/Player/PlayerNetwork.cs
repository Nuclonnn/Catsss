using System;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerController;

        public event Action RemoteJumped;
        public event Action RemoteDashed;

        private void Reset()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.Jumped += HandleLocalJumped;
            playerController.Dashed += HandleLocalDashed;
        }

        public override void OnNetworkDespawn()
        {
            if (playerController == null)
            {
                return;
            }

            playerController.Jumped -= HandleLocalJumped;
            playerController.Dashed -= HandleLocalDashed;
        }

        private void HandleLocalJumped()
        {
            if (IsOwner)
            {
                SendJumpServerRpc();
            }
        }

        private void HandleLocalDashed()
        {
            if (IsOwner)
            {
                SendDashServerRpc();
            }
        }

        [ServerRpc]
        private void SendJumpServerRpc()
        {
            ReceiveJumpClientRpc();
        }

        [ServerRpc]
        private void SendDashServerRpc()
        {
            ReceiveDashClientRpc();
        }

        [ClientRpc]
        private void ReceiveJumpClientRpc()
        {
            if (!IsOwner)
            {
                RemoteJumped?.Invoke();
            }
        }

        [ClientRpc]
        private void ReceiveDashClientRpc()
        {
            if (!IsOwner)
            {
                RemoteDashed?.Invoke();
            }
        }
    }
}
