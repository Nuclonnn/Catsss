using System.Collections.Generic;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// VFX для зон AntiMagic / Aero: включает prefab при активной зоне, выключает при отключении.
    /// Логика зоны не меняется — только визуальный слой на child Visual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneAreaVfxPresenter : MonoBehaviour
    {
        [Header("Zone (один из двух)")]
        [SerializeField] private AntiMagicZone antiMagicZone;
        [SerializeField] private AeroZone aeroZone;

        [Header("VFX")]
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private Transform vfxRoot;
        [Tooltip("Если включено — куб-заглушка Visual можно отключить и оставить только particles.")]
        [SerializeField] private Renderer[] hideWhenVfxActive;

        private GameObject _vfxInstance;
        private readonly List<ParticleSystem> _particleSystems = new();

        private void Reset()
        {
            if (antiMagicZone == null)
            {
                antiMagicZone = GetComponentInParent<AntiMagicZone>();
            }

            if (aeroZone == null)
            {
                aeroZone = GetComponentInParent<AeroZone>();
            }

            if (vfxRoot == null)
            {
                vfxRoot = transform;
            }
        }

        private void Awake()
        {
            if (antiMagicZone == null)
            {
                antiMagicZone = GetComponentInParent<AntiMagicZone>();
            }

            if (aeroZone == null)
            {
                aeroZone = GetComponentInParent<AeroZone>();
            }

            if (vfxRoot == null)
            {
                vfxRoot = transform;
            }
        }

        private void OnEnable()
        {
            if (antiMagicZone != null)
            {
                antiMagicZone.ZoneActiveChanged += HandleZoneActiveChanged;
            }

            if (aeroZone != null)
            {
                aeroZone.ZoneActiveChanged += HandleZoneActiveChanged;
            }

            ApplyVisualState(IsZoneActive());
        }

        private void OnDisable()
        {
            if (antiMagicZone != null)
            {
                antiMagicZone.ZoneActiveChanged -= HandleZoneActiveChanged;
            }

            if (aeroZone != null)
            {
                aeroZone.ZoneActiveChanged -= HandleZoneActiveChanged;
            }

            SetStubRenderersVisible(true);
            SetVfxActive(false);
        }

        private void HandleZoneActiveChanged(bool isActive)
        {
            ApplyVisualState(isActive);
        }

        private bool IsZoneActive()
        {
            if (antiMagicZone != null)
            {
                return antiMagicZone.IsZoneActive;
            }

            if (aeroZone != null)
            {
                return aeroZone.IsZoneActive;
            }

            return false;
        }

        private void ApplyVisualState(bool isActive)
        {
            SetVfxActive(isActive);
            SetStubRenderersVisible(!isActive || vfxPrefab == null);
        }

        private void SetVfxActive(bool isActive)
        {
            if (vfxPrefab == null)
            {
                return;
            }

            if (isActive)
            {
                EnsureVfxInstance();
            }

            if (_vfxInstance == null)
            {
                return;
            }

            _vfxInstance.SetActive(isActive);

            if (!isActive)
            {
                return;
            }

            RestartParticleSystems();
        }

        private void EnsureVfxInstance()
        {
            if (_vfxInstance != null)
            {
                return;
            }

            Transform parent = vfxRoot != null ? vfxRoot : transform;
            _vfxInstance = Instantiate(vfxPrefab, parent);
            _vfxInstance.transform.localPosition = Vector3.zero;
            _vfxInstance.transform.localRotation = Quaternion.identity;
            _vfxInstance.transform.localScale = Vector3.one;

            _particleSystems.Clear();
            _particleSystems.AddRange(_vfxInstance.GetComponentsInChildren<ParticleSystem>(true));
        }

        private void RestartParticleSystems()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void SetStubRenderersVisible(bool visible)
        {
            if (hideWhenVfxActive == null)
            {
                return;
            }

            foreach (Renderer renderer in hideWhenVfxActive)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
}
