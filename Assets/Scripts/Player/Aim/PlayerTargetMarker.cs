using Catsss.Interaction;
using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>World-space маркер над целью броска. Не должен отключать родителя AimVisualsRoot.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTargetMarker : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Vector3 worldOffset = new(0f, 2f, 0f);
        [SerializeField] private bool billboardToCamera = true;

        private Transform _followTarget;
        private Camera _billboardCamera;
        private InteractableHighlightStub _targetOutline;

        private void Reset()
        {
            if (visualRoot == null || visualRoot == gameObject)
            {
                Canvas canvas = GetComponentInChildren<Canvas>(true);

                if (canvas != null)
                {
                    visualRoot = canvas.gameObject;
                }
            }
        }

        public void Show(NetworkPlayerController target, Camera ownerCamera)
        {
            _billboardCamera = ownerCamera;
            AttachTarget(target);
            SetMarkerVisualActive(true);
            RefreshPosition();
            SetTargetOutlineHighlighted(true);
        }

        public void Hide()
        {
            SetTargetOutlineHighlighted(false);
            SetMarkerVisualActive(false);
            _followTarget = null;
            _targetOutline = null;
            _billboardCamera = null;
        }

        public void RefreshPosition()
        {
            if (_followTarget == null)
            {
                return;
            }

            transform.position = _followTarget.position + worldOffset;

            if (billboardToCamera && _billboardCamera != null)
            {
                Vector3 toCamera = _billboardCamera.transform.position - transform.position;

                if (toCamera.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                }
            }
        }

        private void SetMarkerVisualActive(bool active)
        {
            if (visualRoot != null && visualRoot != gameObject)
            {
                visualRoot.SetActive(active);
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(active);
            }
        }

        private void AttachTarget(NetworkPlayerController target)
        {
            _followTarget = target != null ? target.transform : null;
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
