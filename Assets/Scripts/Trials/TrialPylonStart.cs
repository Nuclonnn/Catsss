using Catsss.Core.Services;
using Catsss.Interaction;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Старт испытания: E выдаёт заряд инициатору и блокирует повторное взаимодействие до финиша.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class TrialPylonStart : NetworkBehaviour, IInteractable, IInteractableViewHost
    {
        [Header("Trial")]
        [SerializeField] private TrialDefinition trial;

        [Header("Interaction")]
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private InteractionPromptSettings promptSettings = new();
        [SerializeField] private InteractableHighlightStub highlightStub;

        [Header("Penalty (фаза B — телепорт при выходе из зоны)")]
        [SerializeField] private Transform penaltyRespawnPoint;

        private readonly NetworkVariable<bool> _isDepleted = new();
        private readonly NetworkVariable<bool> _isTrialActive = new();

        public int TrialId => trial != null ? trial.TrialId : 0;
        public TrialDefinition TrialDefinition => trial;
        public Transform PenaltyRespawnPoint => penaltyRespawnPoint != null ? penaltyRespawnPoint : transform;

        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;
        public InteractionPromptSettings PromptSettings => promptSettings;

        private TrialSessionRegistry Registry =>
            ServiceLocator.TryGet(out TrialSessionRegistry registry) ? registry : null;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (highlightStub == null)
            {
                highlightStub = GetComponentInChildren<InteractableHighlightStub>(true);
            }
        }

        public override void OnNetworkSpawn()
        {
            _isDepleted.OnValueChanged += OnDepletedChanged;
            Registry?.RegisterPylon(this);
            RefreshPromptVisibility();
        }

        public override void OnNetworkDespawn()
        {
            Registry?.UnregisterPylon(this);
            _isDepleted.OnValueChanged -= OnDepletedChanged;
        }

        public bool CanInteract(NetworkPlayerController interactor)
        {
            if (!IsSpawned)
            {
                return false;
            }

            if (_isDepleted.Value || _isTrialActive.Value)
            {
                return false;
            }

            if (interactor != null && interactor.TryGetComponent(out PlayerChargeController charge)
                && charge.ChargeId != 0)
            {
                return false;
            }

            return true;
        }

        public void RequestInteract(NetworkPlayerController interactor)
        {
            if (interactor == null || !interactor.IsOwner)
            {
                return;
            }

            if (!interactor.TryGetComponent(out PlayerTrialInteractor trialInteractor))
            {
                Debug.LogWarning("[TrialPylonStart] На игроке нет PlayerTrialInteractor.", interactor);
                return;
            }

            trialInteractor.RequestStartTrial(this);
        }

        /// <summary>Только сервер, вызывается из <see cref="PlayerTrialInteractor"/>.</summary>
        public void HandleInteractServer(NetworkPlayerController controller)
        {
            if (!IsServer || _isDepleted.Value || _isTrialActive.Value || controller == null)
            {
                return;
            }

            TrialSessionRegistry registry = Registry;

            if (registry == null)
            {
                Debug.LogWarning("[TrialPylonStart] TrialSessionRegistry не найден на сцене.", this);
                return;
            }

            if (trial == null || !trial.IsValid)
            {
                Debug.LogWarning("[TrialPylonStart] Не назначен или невалиден Trial Definition.", this);
                return;
            }

            if (registry.TryBeginTrial(trial, controller))
            {
                _isTrialActive.Value = true;
            }
        }

        public void ResetTrialActiveServer()
        {
            if (!IsServer || _isDepleted.Value)
            {
                return;
            }

            _isTrialActive.Value = false;
        }

        public void MarkCompletedServer()
        {
            if (!IsServer)
            {
                return;
            }

            _isTrialActive.Value = false;
            _isDepleted.Value = true;
        }

        private void OnDepletedChanged(bool previous, bool current)
        {
            RefreshPromptVisibility();
        }

        private void RefreshPromptVisibility()
        {
        }

        InteractableHighlightStub IInteractableViewHost.HighlightStub => highlightStub;
    }
}
