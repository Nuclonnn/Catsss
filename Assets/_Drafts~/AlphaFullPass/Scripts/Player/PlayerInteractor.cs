using Catsss.Gameplay.PuzzleBlocks;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField, Min(0.1f)] private float interactRadius = 2f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        public override void OnNetworkSpawn()
        {
            if (_playerController != null)
            {
                _playerController.Interacted += TryInteract;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_playerController != null)
            {
                _playerController.Interacted -= TryInteract;
            }
        }

        private void TryInteract()
        {
            if (!IsOwner)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableMask, QueryTriggerInteraction.Collide);
            MagicSeal closestSeal = null;
            float closestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                MagicSeal seal = hit.GetComponentInParent<MagicSeal>();
                if (seal == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, seal.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSeal = seal;
                }
            }

            if (closestSeal != null)
            {
                closestSeal.RequestStandardActivation(NetworkObject);
            }
        }
    }
}
