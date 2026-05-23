using UnityEngine;

namespace Catsss.Player
{
    /// <summary>Заглушка VFX заряда: меняет цвет на дочерних Renderer'ах при смене ChargeId.</summary>
    public sealed class PlayerChargeVisualStub : MonoBehaviour
    {
        [SerializeField] private PlayerChargeController chargeController;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private string colorPropertyName = "_BaseColor";

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;

        private void Awake()
        {
            if (chargeController == null)
            {
                chargeController = GetComponentInParent<PlayerChargeController>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            _colorPropertyId = Shader.PropertyToID(colorPropertyName);
        }

        private void OnEnable()
        {
            if (chargeController != null)
            {
                chargeController.ChargeChanged += OnChargeChanged;
            }

            Apply(chargeController != null ? chargeController.ChargeId : (byte)0);
        }

        private void OnDisable()
        {
            if (chargeController != null)
            {
                chargeController.ChargeChanged -= OnChargeChanged;
            }
        }

        private void OnChargeChanged(byte chargeId, int trialId)
        {
            Apply(chargeId);
        }

        private void Apply(byte chargeId)
        {
            Color tint = Color.white;

            if (chargeId != 0 && chargeController != null && chargeController.ActiveDefinition != null)
            {
                tint = chargeController.ActiveDefinition.AuraColor;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_colorPropertyId, tint);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
