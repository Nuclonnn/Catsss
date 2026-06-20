using Catsss.Player.Aim;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Dev
{
    /// <summary>Dev-only: логи Aim / Throw для проверки Stage 4. Не вешать на shipping-префабы.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAimController))]
    public sealed class PlayerAimDebugLog : NetworkBehaviour
    {
        [SerializeField] private PlayerAimController aimController;

        private void Reset()
        {
            aimController = GetComponent<PlayerAimController>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner || aimController == null)
            {
                enabled = false;
                return;
            }

            aimController.AimEntered += OnAimEntered;
            aimController.AimExited += OnAimExited;
            aimController.ThrowCommitted += OnThrowCommitted;
        }

        public override void OnNetworkDespawn()
        {
            if (aimController == null)
            {
                return;
            }

            aimController.AimEntered -= OnAimEntered;
            aimController.AimExited -= OnAimExited;
            aimController.ThrowCommitted -= OnThrowCommitted;
        }

        private void OnAimEntered()
        {
            Debug.Log("[PlayerAim] AimEntered", this);
        }

        private void OnAimExited()
        {
            Debug.Log("[PlayerAim] AimExited", this);
        }

        private void OnThrowCommitted(Vector3 origin, Vector3 direction, ulong targetClientId)
        {
            Debug.Log(
                $"[PlayerAim] ThrowCommitted (RPC отправлен), target={targetClientId}, origin={origin}",
                this);
        }
    }
}
