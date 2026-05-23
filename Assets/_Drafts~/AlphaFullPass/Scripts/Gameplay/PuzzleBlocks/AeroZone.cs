using Catsss.Player;
using UnityEngine;

namespace Catsss.Gameplay.PuzzleBlocks
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroZone : MonoBehaviour
    {
        [SerializeField] private Vector3 forceDirection = Vector3.up;
        [SerializeField, Min(0f)] private float normalForce = 12f;
        [SerializeField, Min(0f)] private float airChargeForce = 35f;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController == null || !playerController.IsOwner || playerController.IsHeavy)
            {
                return;
            }

            float force = playerController.ActiveCharge != null && !playerController.ActiveCharge.IsHeavy
                ? airChargeForce
                : normalForce;

            playerController.AddExternalForce(forceDirection.normalized * force, ForceMode.Acceleration);
        }
    }
}
