using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Визуальная заглушка печати: красит Renderer'ы по реплицированному состоянию MagicSeal.</summary>
    public sealed class MagicSealVisualStub : MonoBehaviour
    {
        [SerializeField] private MagicSeal seal;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color pressedColor = new(0.2f, 1f, 0.55f, 1f);
        [SerializeField] private Color lockedColor = new(0.4f, 0.75f, 1f, 1f);
        [SerializeField] private Color disabledColor = new(0.25f, 0.25f, 0.25f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            _propertyBlock = new MaterialPropertyBlock();
            _colorPropertyId = Shader.PropertyToID(colorPropertyName);
        }

        private void OnEnable()
        {
            if (seal != null)
            {
                seal.StateChanged += OnSealStateChanged;
                Apply(seal.State);
            }
        }

        private void OnDisable()
        {
            if (seal != null)
            {
                seal.StateChanged -= OnSealStateChanged;
            }
        }

        private void OnSealStateChanged(MagicSealState previous, MagicSealState current)
        {
            Apply(current);
        }

        private void ResolveReferences()
        {
            if (seal == null)
            {
                seal = GetComponentInParent<MagicSeal>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Apply(MagicSealState state)
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            Color tint = state switch
            {
                MagicSealState.Pressed => pressedColor,
                MagicSealState.Locked => lockedColor,
                MagicSealState.Disabled => disabledColor,
                _ => idleColor,
            };

            if (renderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_colorPropertyId, tint);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
