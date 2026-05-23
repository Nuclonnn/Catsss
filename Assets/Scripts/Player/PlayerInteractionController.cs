using Catsss.Core.WorldHints;
using Catsss.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>
    /// Владелец: ближайший IInteractable в радиусе от игрока, подсветка, промпт, E.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : NetworkBehaviour
    {
        [Header("Detection")]
        [SerializeField] private NetworkPlayerController playerController;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField, Min(0.5f)] private float interactionRange = 3f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private Vector3 lineOfSightOriginOffset = new(0f, 0.9f, 0f);
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Prompt")]
        [SerializeField] private WorldTextHintPresenter hintPresenter;

        private IInteractable _focusedInteractable;
        private InteractableHighlightStub _focusedHighlight;
        private bool _loggedMissingPresenter;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<NetworkPlayerController>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (hintPresenter == null)
            {
                hintPresenter = GetComponent<WorldTextHintPresenter>();
            }
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;

            if (IsOwner)
            {
                ResolveHintPresenterReference();
            }
        }

        private void ResolveHintPresenterReference()
        {
            if (hintPresenter != null)
            {
                return;
            }

            hintPresenter = GetComponent<WorldTextHintPresenter>();

            if (hintPresenter == null && !_loggedMissingPresenter)
            {
                _loggedMissingPresenter = true;
                Debug.LogWarning(
                    "[PlayerInteractionController] Нет WorldTextHintPresenter на PlayerRoot.",
                    this);
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            UpdateFocus();

            if (inputReader != null && inputReader.ConsumeInteractPressedThisFrame() && _focusedInteractable != null)
            {
                _focusedInteractable.RequestInteract(playerController);
            }
        }

        private void UpdateFocus()
        {
            SetFocus(FindClosestInteractable());
        }

        private IInteractable FindClosestInteractable()
        {
            Vector3 playerPosition = transform.position;
            Collider[] hits = Physics.OverlapSphere(
                playerPosition,
                interactionRange,
                interactableMask,
                QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();

                if (interactable == null)
                {
                    continue;
                }

                Transform anchor = interactable.PromptAnchor;
                Vector3 targetPosition = anchor != null ? anchor.position : hit.transform.position;
                float distance = Vector3.Distance(playerPosition, targetPosition);

                if (distance > interactionRange)
                {
                    continue;
                }

                if (requireLineOfSight && !HasLineOfSight(targetPosition, hit.transform))
                {
                    continue;
                }

                if (playerController != null && !interactable.CanInteract(playerController))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = interactable;
                }
            }

            return best;
        }

        private bool HasLineOfSight(Vector3 targetPosition, Transform interactableRoot)
        {
            Vector3 origin = transform.position + lineOfSightOriginOffset;
            Vector3 toTarget = targetPosition - origin;
            float distance = toTarget.magnitude;

            if (distance < 0.05f)
            {
                return true;
            }

            Vector3 direction = toTarget / distance;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == interactableRoot || hit.transform.IsChildOf(interactableRoot);
        }

        private void SetFocus(IInteractable interactable)
        {
            if (_focusedInteractable == interactable)
            {
                return;
            }

            if (_focusedHighlight != null)
            {
                _focusedHighlight.SetHighlighted(false);
            }

            _focusedInteractable = interactable;
            _focusedHighlight = null;

            if (interactable == null)
            {
                hintPresenter?.ClearInteractionFocus();
                return;
            }

            if (interactable is IInteractableViewHost viewHost)
            {
                _focusedHighlight = viewHost.HighlightStub;
            }
            else if (interactable is MonoBehaviour behaviour)
            {
                _focusedHighlight = behaviour.GetComponent<InteractableHighlightStub>();
            }

            if (_focusedHighlight != null)
            {
                _focusedHighlight.SetHighlighted(true);
            }

            if (hintPresenter == null)
            {
                ResolveHintPresenterReference();
            }

            hintPresenter?.SetInteractionFocus(interactable.PromptAnchor, interactable.PromptSettings);
        }
    }
}
