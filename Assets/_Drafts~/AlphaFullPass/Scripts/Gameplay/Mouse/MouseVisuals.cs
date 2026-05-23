using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    public sealed class MouseVisuals : MonoBehaviour
    {
        [SerializeField] private MouseBrain mouseBrain;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material spectralMaterial;
        [SerializeField] private Material physicalMaterial;

        private static readonly int CaughtHash = Animator.StringToHash("Caught");

        private void OnEnable()
        {
            if (mouseBrain != null)
            {
                mouseBrain.Materialized += ApplyPhysicalLook;
                mouseBrain.Caught += PlayCaught;
            }
        }

        private void OnDisable()
        {
            if (mouseBrain != null)
            {
                mouseBrain.Materialized -= ApplyPhysicalLook;
                mouseBrain.Caught -= PlayCaught;
            }
        }

        private void Start()
        {
            ApplyMaterial(spectralMaterial);
        }

        private void ApplyPhysicalLook()
        {
            ApplyMaterial(physicalMaterial);
        }

        private void PlayCaught()
        {
            if (animator != null)
            {
                animator.SetTrigger(CaughtHash);
            }
        }

        private void ApplyMaterial(Material material)
        {
            if (material == null || renderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.material = material;
                }
            }
        }
    }
}
