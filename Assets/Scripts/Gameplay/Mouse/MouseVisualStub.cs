using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Визуальная заглушка мыши: fade in/out, spectral/physical tint через MaterialPropertyBlock.</summary>
    public sealed class MouseVisualStub : MonoBehaviour
    {
        [SerializeField] private MouseBrain mouseBrain;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private Color spectralColor = new(0.55f, 0.85f, 1f, 0.45f);
        [SerializeField] private Color physicalColor = new(0.75f, 0.55f, 0.35f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private float _displayAlpha;
        private float _targetAlpha;
        private Color _targetColor;

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
            ResolveReferences();

            if (mouseBrain == null)
            {
                return;
            }

            mouseBrain.PresenceModeChanged += OnPresenceModeChanged;
            SetTargets(mouseBrain.PresenceMode);
            _displayAlpha = _targetAlpha;
            ApplyTintImmediate();
        }

        private void OnDisable()
        {
            if (mouseBrain != null)
            {
                mouseBrain.PresenceModeChanged -= OnPresenceModeChanged;
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(_displayAlpha, _targetAlpha))
            {
                return;
            }

            float fadeDuration = ResolveFadeDuration();
            _displayAlpha = Mathf.MoveTowards(_displayAlpha, _targetAlpha, Time.deltaTime / fadeDuration);
            ApplyTintImmediate();
        }

        private void OnPresenceModeChanged(MousePresenceMode previous, MousePresenceMode current)
        {
            SetTargets(current);
        }

        private void SetTargets(MousePresenceMode mode)
        {
            switch (mode)
            {
                case MousePresenceMode.Physical:
                    _targetColor = physicalColor;
                    _targetAlpha = physicalColor.a;
                    break;
                case MousePresenceMode.Spectral:
                    _targetColor = spectralColor;
                    _targetAlpha = spectralColor.a;
                    break;
                default:
                    _targetColor = spectralColor;
                    _targetAlpha = 0f;
                    break;
            }

            UpdateRendererVisibility();
        }

        private void ResolveReferences()
        {
            if (mouseBrain == null)
            {
                mouseBrain = GetComponentInParent<MouseBrain>();
            }

            if (!HasValidRenderers())
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private bool HasValidRenderers()
        {
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer != null)
                {
                    return true;
                }
            }

            return false;
        }

        private float ResolveFadeDuration()
        {
            if (mouseBrain != null && mouseBrain.Config != null)
            {
                return mouseBrain.Config.PresenceFadeDuration;
            }

            return 0.25f;
        }

        private void UpdateRendererVisibility()
        {
            if (renderers == null)
            {
                return;
            }

            bool shouldShow = _targetAlpha > 0f || _displayAlpha > 0.001f;

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = shouldShow;
                }
            }
        }

        private void ApplyTintImmediate()
        {
            if (renderers == null)
            {
                return;
            }

            UpdateRendererVisibility();

            Color color = _targetColor;
            color.a *= _displayAlpha;

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_colorPropertyId, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
