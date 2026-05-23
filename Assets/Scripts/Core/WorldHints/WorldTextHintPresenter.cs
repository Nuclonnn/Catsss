using System;
using Catsss.Core.Localization;
using Catsss.Interaction;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>
    /// На PlayerRoot (только owner): одна подсказка, переезжает между якорями по приоритету.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldTextHintPresenter : NetworkBehaviour
    {
        public const int InteractionPriority = 10;

        [Header("View")]
        [SerializeField] private WorldTextHintView hintView;

        [Header("Camera")]
        [SerializeField] private PlayerCameraController cameraController;

        [Header("Anchors")]
        [SerializeField] private Transform headAnchor;
        [SerializeField] private Vector3 headOffset = new(0f, 1.75f, 0f);

        private HintSlot _interactionSlot;
        private HintSlot _triggerSlot;

        private void Awake()
        {
            if (hintView == null)
            {
                hintView = GetComponentInChildren<WorldTextHintView>(true);
            }

            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<PlayerCameraController>(true);
            }

            EnsureHeadAnchor();
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;

            if (!IsOwner)
            {
                return;
            }

            ResolveCameraReference();
            RefreshDisplay();
        }

        public void SetInteractionFocus(Transform anchor, InteractionPromptSettings settings)
        {
            if (!IsOwner)
            {
                return;
            }

            _interactionSlot = new HintSlot
            {
                IsActive = anchor != null && settings != null,
                Anchor = anchor,
                Offset = hintView != null ? hintView.DefaultLocalOffset : new Vector3(0f, 1.4f, 0f),
                Text = settings?.Text,
                Priority = InteractionPriority,
            };
            RefreshDisplay();
        }

        public void ClearInteractionFocus()
        {
            if (!IsOwner)
            {
                return;
            }

            _interactionSlot = default;
            RefreshDisplay();
        }

        public bool TryShowTriggerHint(object owner, WorldTextHintDefinition definition, Transform customAnchor)
        {
            if (!IsOwner || owner == null || definition == null)
            {
                return false;
            }

            if (_triggerSlot.IsActive
                && !ReferenceEquals(_triggerSlot.Owner, owner)
                && _triggerSlot.Priority > definition.Priority)
            {
                return false;
            }

            if (!TryResolveAnchor(definition.AnchorMode, customAnchor, out Transform anchor))
            {
                return false;
            }

            _triggerSlot = new HintSlot
            {
                Owner = owner,
                IsActive = true,
                Anchor = anchor,
                Offset = definition.LocalOffset,
                Text = definition.Text,
                Priority = definition.Priority,
            };
            RefreshDisplay();
            return true;
        }

        public void HideTriggerHint(object owner)
        {
            if (!IsOwner || !_triggerSlot.IsActive || !ReferenceEquals(_triggerSlot.Owner, owner))
            {
                return;
            }

            _triggerSlot = default;
            RefreshDisplay();
        }

        public bool IsTriggerHintVisible(object owner)
        {
            return _triggerSlot.IsActive && ReferenceEquals(_triggerSlot.Owner, owner);
        }

        public Transform ResolveAnchor(WorldTextHintAnchorMode mode, Transform customAnchor)
        {
            TryResolveAnchor(mode, customAnchor, out Transform anchor);
            return anchor;
        }

        private bool TryResolveAnchor(WorldTextHintAnchorMode mode, Transform customAnchor, out Transform anchor)
        {
            switch (mode)
            {
                case WorldTextHintAnchorMode.LocalPlayerRoot:
                    anchor = transform;
                    return true;

                case WorldTextHintAnchorMode.LocalPlayerHead:
                    EnsureHeadAnchor();
                    anchor = headAnchor != null ? headAnchor : transform;
                    return true;

                case WorldTextHintAnchorMode.CustomTransform:
                default:
                    anchor = customAnchor;
                    return anchor != null;
            }
        }

        private void EnsureHeadAnchor()
        {
            if (headAnchor != null)
            {
                return;
            }

            var headObject = new GameObject("WorldHintHeadAnchor");
            headAnchor = headObject.transform;
            headAnchor.SetParent(transform, false);
            headAnchor.localPosition = headOffset;
        }

        private void ResolveCameraReference()
        {
            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<PlayerCameraController>(true);
            }

            Camera camera = cameraController != null ? cameraController.UnityCamera : null;
            hintView?.SetWorldCamera(camera);
        }

        private void RefreshDisplay()
        {
            if (!IsOwner)
            {
                return;
            }

            if (hintView == null)
            {
                hintView = GetComponentInChildren<WorldTextHintView>(true);
            }

            if (hintView == null)
            {
                return;
            }

            ResolveCameraReference();

            HintSlot winner = ChooseWinner();

            if (!winner.IsActive || winner.Anchor == null || winner.Text == null)
            {
                hintView.Clear();
                return;
            }

            hintView.Bind(winner.Anchor, winner.Offset, winner.Text);
        }

        private HintSlot ChooseWinner()
        {
            if (_triggerSlot.IsActive && _interactionSlot.IsActive)
            {
                return _triggerSlot.Priority >= _interactionSlot.Priority ? _triggerSlot : _interactionSlot;
            }

            if (_triggerSlot.IsActive)
            {
                return _triggerSlot;
            }

            if (_interactionSlot.IsActive)
            {
                return _interactionSlot;
            }

            return default;
        }

        private struct HintSlot
        {
            public object Owner;
            public bool IsActive;
            public Transform Anchor;
            public Vector3 Offset;
            public LocalizedTextReference Text;
            public int Priority;
        }
    }
}
