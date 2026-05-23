using System.Collections.Generic;
using Catsss.Rendering;
using UnityEngine;

namespace Catsss.Interaction
{
    /// <summary>
    /// Включает URP Rendering Layer для контура (см. InteractableOutlineRendererFeature на PC_Renderer).
    /// </summary>
    public sealed class InteractableHighlightStub : MonoBehaviour
    {
        private static readonly int ExtrusionCenterOsId = Shader.PropertyToID("_ExtrusionCenterOS");

        [SerializeField] private Renderer[] sourceRenderers;
        [SerializeField] private uint outlineRenderingLayerMask = InteractableOutlineRendering.LayerMask;

        private readonly Dictionary<Renderer, uint> _originalRenderingLayerMasks = new();
        private MaterialPropertyBlock _propertyBlock;
        private bool _isHighlighted;

        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                _propertyBlock ??= new MaterialPropertyBlock();
                return _propertyBlock;
            }
        }

        private void Reset()
        {
            if (sourceRenderers == null || sourceRenderers.Length == 0)
            {
                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

                if (meshRenderer != null)
                {
                    sourceRenderers = new[] { meshRenderer };
                }
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
            {
                return;
            }

            _isHighlighted = highlighted;

            if (sourceRenderers == null || sourceRenderers.Length == 0)
            {
                Debug.LogWarning("[InteractableHighlightStub] Укажи Source Renderers.", this);
                return;
            }

            if (highlighted)
            {
                EnableOutline();
                return;
            }

            DisableOutline();
        }

        private void EnableOutline()
        {
            foreach (Renderer renderer in sourceRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!_originalRenderingLayerMasks.ContainsKey(renderer))
                {
                    _originalRenderingLayerMasks[renderer] = renderer.renderingLayerMask;
                }

                renderer.renderingLayerMask = _originalRenderingLayerMasks[renderer] | outlineRenderingLayerMask;

                // Центр bounds в object space — радиальное раздувание без дыр на углах бокса.
                MaterialPropertyBlock block = PropertyBlock;
                renderer.GetPropertyBlock(block);
                block.SetVector(ExtrusionCenterOsId, renderer.localBounds.center);
                renderer.SetPropertyBlock(block);
            }
        }

        private void DisableOutline()
        {
            foreach (Renderer renderer in sourceRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (_originalRenderingLayerMasks.TryGetValue(renderer, out uint originalMask))
                {
                    renderer.renderingLayerMask = originalMask;
                }
            }
        }

        private void OnDisable()
        {
            if (_isHighlighted)
            {
                DisableOutline();
                _isHighlighted = false;
            }
        }
    }
}
