using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Полупрозрачная заглушка завесы/тумана. Логика зоны не зависит от визуала; позже заменить на particles/mesh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AntiMagicZoneVisualStub : MonoBehaviour
    {
        [SerializeField] private AntiMagicZone zone;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private Color baseColor = new(0.55f, 0.15f, 1f, 0.32f);
        [SerializeField, Range(0f, 0.08f)] private float disabledAlpha = 0.02f;
        [SerializeField] private float pulseAmplitude = 0.08f;
        [SerializeField] private float pulseSpeed = 1.2f;
        [SerializeField] private bool animatePulse = true;

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private bool _zoneVisuallyActive = true;

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
            if (zone != null)
            {
                zone.ZoneActiveChanged += HandleZoneActiveChanged;
                _zoneVisuallyActive = zone.IsZoneActive;
            }

            ApplyCurrentColor();
        }

        private void OnDisable()
        {
            if (zone != null)
            {
                zone.ZoneActiveChanged -= HandleZoneActiveChanged;
            }
        }

        private void Update()
        {
            ApplyCurrentColor();
        }

        private void HandleZoneActiveChanged(bool isActive)
        {
            _zoneVisuallyActive = isActive;
        }

        private void ApplyCurrentColor()
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (!_zoneVisuallyActive)
            {
                Color hidden = baseColor;
                hidden.a = disabledAlpha;
                ApplyColor(hidden);
                return;
            }

            float alpha = baseColor.a;

            if (animatePulse)
            {
                alpha += Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            }

            Color color = baseColor;
            color.a = Mathf.Clamp01(alpha);
            ApplyColor(color);
        }

        private void ResolveReferences()
        {
            if (zone == null)
            {
                zone = GetComponentInParent<AntiMagicZone>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void ApplyColor(Color color)
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
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
