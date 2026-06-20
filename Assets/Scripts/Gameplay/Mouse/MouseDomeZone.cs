using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>
    /// Trigger-зона купола: переводит мышь в физический режим и DomeFlee (если ещё не в куполе).
    /// Дублирует MouseRoute EndMode EnterDome как запасной вход.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MouseDomeZone : MonoBehaviour
    {
        [SerializeField] private MouseBrain mouse;
        [SerializeField] private bool playOnce = true;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new(0.55f, 0.25f, 1f, 0.25f);

        private bool _hasTriggered;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerAuthority())
            {
                return;
            }

            if (mouse == null)
            {
                mouse = other.GetComponentInParent<MouseBrain>();

                if (mouse == null)
                {
                    return;
                }
            }

            if (other.GetComponentInParent<MouseBrain>() != mouse)
            {
                return;
            }

            if (playOnce && _hasTriggered)
            {
                return;
            }

            if (mouse.IsInDomeFlee)
            {
                return;
            }

            if (playOnce)
            {
                _hasTriggered = true;
            }

            mouse.EnterDomeServer();
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo)
            {
                return;
            }

            Collider col = GetComponent<Collider>();

            if (col == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.matrix = col.transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
