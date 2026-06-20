using UnityEngine;

namespace Catsss.Gameplay.Charges.Projectile
{
    /// <summary>
    /// Заглушка telegraph на ловце: статичный оттенок emission без пульсации.
    /// Позже заменить/дополнить VFX, Animator, Audio по событиям индикатора.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIncomingChargeVisualStub : MonoBehaviour
    {
        [SerializeField] private PlayerIncomingChargeIndicator incomingIndicator;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private string emissionColorPropertyName = "_EmissionColor";
        [SerializeField] private Color incomingEmissionTint = new(0.45f, 0.35f, 0.9f);
        [SerializeField] private float incomingEmissionIntensity = 0.35f;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges;

        private MaterialPropertyBlock _propertyBlock;
        private int _emissionColorPropertyId;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (incomingIndicator == null)
            {
                incomingIndicator = GetComponentInParent<PlayerIncomingChargeIndicator>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            _emissionColorPropertyId = Shader.PropertyToID(emissionColorPropertyName);
        }

        private void OnEnable()
        {
            if (incomingIndicator == null)
            {
                return;
            }

            incomingIndicator.IsIncomingTargetChanged += OnIncomingTargetChanged;
            incomingIndicator.IncomingThrowerClientIdChanged += OnIncomingThrowerChanged;
            ApplyVisual(incomingIndicator.HasIncomingCharge);
        }

        private void OnDisable()
        {
            if (incomingIndicator == null)
            {
                return;
            }

            incomingIndicator.IsIncomingTargetChanged -= OnIncomingTargetChanged;
            incomingIndicator.IncomingThrowerClientIdChanged -= OnIncomingThrowerChanged;
            ApplyVisual(false);
        }

        private void OnIncomingTargetChanged(bool isIncoming)
        {
            ApplyVisual(isIncoming);
        }

        private void OnIncomingThrowerChanged(ulong throwerClientId)
        {
            if (logStateChanges)
            {
                Debug.Log(
                    $"[IncomingChargeVisualStub] throwerClientId={throwerClientId}, active={throwerClientId != 0}",
                    this);
            }
        }

        private void ApplyVisual(bool isIncoming)
        {
            Color emission = isIncoming ? incomingEmissionTint * incomingEmissionIntensity : Color.black;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_emissionColorPropertyId, emission);

                if (_emissionColorPropertyId != EmissionColorId)
                {
                    _propertyBlock.SetColor(EmissionColorId, emission);
                }

                renderer.SetPropertyBlock(_propertyBlock);
            }

            if (logStateChanges)
            {
                Debug.Log($"[IncomingChargeVisualStub] incoming={isIncoming}", this);
            }
        }
    }
}
