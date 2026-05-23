using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlatformCollisionHandler : MonoBehaviour
{
        PlatformMover currentPlatformMover;

        public bool IsOnMovingPlatform => currentPlatformMover != null;
        public Vector3 CurrentPlatformVelocity =>
            currentPlatformMover != null ? currentPlatformMover.PlatformVelocity : Vector3.zero;
        public Vector3 CurrentPlatformDelta =>
            currentPlatformMover != null ? currentPlatformMover.PlatformDelta : Vector3.zero;

        void OnCollisionStay(Collision other) {
            if (!other.gameObject.CompareTag("MovingPlatform")) return;

            if (!HasTopContact(other)) {
                if (currentPlatformMover != null && other.gameObject == currentPlatformMover.gameObject) {
                    currentPlatformMover = null;
                }
                return;
            }

            if (other.gameObject.TryGetComponent(out PlatformMover platformMover)) {
                currentPlatformMover = platformMover;
            }
        }

        void OnCollisionExit(Collision other) {
            if (!other.gameObject.CompareTag("MovingPlatform")) return;
            if (currentPlatformMover != null && other.gameObject == currentPlatformMover.gameObject) {
                currentPlatformMover = null;
            }
        }

        bool HasTopContact(Collision collision) {
            for (int i = 0; i < collision.contactCount; i++) {
                if (collision.GetContact(i).normal.y > 0.5f) {
                    return true;
                }
            }

            return false;
        }
}
