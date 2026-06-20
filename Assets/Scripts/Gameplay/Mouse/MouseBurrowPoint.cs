using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Маркер точки появления/исчезновения мыши. Расставляется в сцене вручную.</summary>
    public sealed class MouseBurrowPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new(0.95f, 0.55f, 0.15f, 0.9f);

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Vector3 center = transform.position;
            float size = 0.25f;
            Gizmos.DrawWireSphere(center, size);
            Gizmos.DrawLine(center, center + transform.forward * (size * 2f));
        }
    }
}
