using Catsss.Interaction;
using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>Жёлтый контур напарника в Aim Mode. Без world-маркера / крестика.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTargetMarker : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;

        private InteractableHighlightStub _targetOutline;

        private void Awake()
        {
            HideLegacyMarkerVisual();
        }

        public void Show(NetworkPlayerController target, Camera ownerCamera)
        {
            HideLegacyMarkerVisual();
            AttachTarget(target);
            SetTargetOutlineHighlighted(true);
        }

        public void Hide()
        {
            SetTargetOutlineHighlighted(false);
            _targetOutline = null;
            HideLegacyMarkerVisual();
        }

        public void RefreshPosition()
        {
            // Контур на модели напарника — отдельный world-маркер не нужен.
        }

        private void HideLegacyMarkerVisual()
        {
            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(false);
            }
        }

        private void AttachTarget(NetworkPlayerController target)
        {
            _targetOutline = null;

            if (target == null)
            {
                return;
            }

            _targetOutline = target.GetComponentInChildren<InteractableHighlightStub>(true);
        }

        private void SetTargetOutlineHighlighted(bool highlighted)
        {
            _targetOutline?.SetHighlighted(highlighted);
        }
    }
}
