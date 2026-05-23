using System;
using Catsss.Core.Events;
using Catsss.Player;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>
    /// Настраиваемый триггер подсказки: подписывается на SO-события, показывает текст на локальном игроке.
    /// Можно вынести на префаб / объект сцены и переиспользовать Definition в других проектах.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldTextHintTrigger : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private WorldTextHintDefinition definition;
        [SerializeField] private Transform customAnchor;
        [SerializeField] private WorldTextHintAnchor anchorMarker;

        [Header("Optional overrides (если Definition = null)")]
        [SerializeField] private bool useInlineOverrides;
        [SerializeField] private WorldTextHintShowRule inlineShowRule;
        [SerializeField] private WorldTextHintHideRule inlineHideRule;

        private bool _isShowing;
        private bool _runtimeEventSeen;
        private bool _previousCanDash;
        private float _showStartedAt = -1f;
        private WorldTextHintPresenter _presenter;
        private PlayerInputReader _inputReader;
        private PlayerPermanentModifiers _permanentModifiers;

        private void OnEnable()
        {
            SubscribeShowSources();

            if (GetEffectiveShowRule().Kind == WorldTextHintShowKind.OnEnable)
            {
                TryShow();
            }
        }

        private void OnDisable()
        {
            UnsubscribeShowSources();

            if (GetEffectiveHideRule().Kind == WorldTextHintHideKind.OnDisable)
            {
                Hide();
            }
            else
            {
                Hide();
            }
        }

        private void Update()
        {
            if (GetEffectiveShowRule().Kind == WorldTextHintShowKind.PlayerCanDashUnlocked && _permanentModifiers == null)
            {
                BindPermanentModifiersListener();
            }

            if (!_isShowing)
            {
                return;
            }

            if (!EnsureLocalPlayerRefs())
            {
                return;
            }

            EvaluateHideRules();
        }

        /// <summary>Ручной показ (Show Kind = Manual).</summary>
        public void ShowManual()
        {
            TryShow(forceManual: true);
        }

        public void HideManual()
        {
            Hide();
        }

        private void SubscribeShowSources()
        {
            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            switch (showRule.Kind)
            {
                case WorldTextHintShowKind.TrialProgress:
                    if (showRule.TrialProgressChannel != null)
                    {
                        showRule.TrialProgressChannel.Raised += OnTrialProgressRaised;
                    }

                    break;

                case WorldTextHintShowKind.EmptyEvent:
                    if (showRule.EmptyEventChannel != null)
                    {
                        showRule.EmptyEventChannel.Raised += OnEmptyEventRaised;
                    }

                    break;

                case WorldTextHintShowKind.PlayerCanDashUnlocked:
                    BindPermanentModifiersListener();
                    break;
            }

            WorldTextHintHideRule hideRule = GetEffectiveHideRule();

            if (hideRule.Kind == WorldTextHintHideKind.TrialProgress && hideRule.TrialProgressChannel != null)
            {
                hideRule.TrialProgressChannel.Raised += OnTrialProgressRaised;
            }

            if (hideRule.Kind == WorldTextHintHideKind.EmptyEvent && hideRule.EmptyEventChannel != null)
            {
                hideRule.EmptyEventChannel.Raised += OnEmptyEventRaised;
            }
        }

        private void UnsubscribeShowSources()
        {
            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            if (showRule.TrialProgressChannel != null)
            {
                showRule.TrialProgressChannel.Raised -= OnTrialProgressRaised;
            }

            if (showRule.EmptyEventChannel != null)
            {
                showRule.EmptyEventChannel.Raised -= OnEmptyEventRaised;
            }

            WorldTextHintHideRule hideRule = GetEffectiveHideRule();

            if (hideRule.TrialProgressChannel != null)
            {
                hideRule.TrialProgressChannel.Raised -= OnTrialProgressRaised;
            }

            if (hideRule.EmptyEventChannel != null)
            {
                hideRule.EmptyEventChannel.Raised -= OnEmptyEventRaised;
            }

            UnbindPermanentModifiersListener();
        }

        private void OnTrialProgressRaised(TrialProgressEvent progressEvent)
        {
            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            if (!_isShowing && showRule.Kind == WorldTextHintShowKind.TrialProgress && showRule.MatchesTrialProgress(in progressEvent))
            {
                _runtimeEventSeen = true;
                TryShow();
            }

            WorldTextHintHideRule hideRule = GetEffectiveHideRule();

            if (_isShowing && hideRule.Kind == WorldTextHintHideKind.TrialProgress && hideRule.MatchesTrialProgress(in progressEvent))
            {
                Hide();
            }
        }

        private void OnEmptyEventRaised(EmptyEvent _)
        {
            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            if (!_isShowing && showRule.Kind == WorldTextHintShowKind.EmptyEvent)
            {
                _runtimeEventSeen = true;
                TryShow();
            }

            WorldTextHintHideRule hideRule = GetEffectiveHideRule();

            if (_isShowing && hideRule.Kind == WorldTextHintHideKind.EmptyEvent)
            {
                Hide();
            }
        }

        private void BindPermanentModifiersListener()
        {
            if (!EnsureLocalPlayerRefs() || _permanentModifiers == null)
            {
                return;
            }

            UnbindPermanentModifiersListener();
            _previousCanDash = _permanentModifiers.CanDash;
            _permanentModifiers.ModifiersChanged += OnPermanentModifiersChanged;
        }

        private void UnbindPermanentModifiersListener()
        {
            if (_permanentModifiers != null)
            {
                _permanentModifiers.ModifiersChanged -= OnPermanentModifiersChanged;
            }
        }

        private void OnPermanentModifiersChanged()
        {
            if (_permanentModifiers == null)
            {
                return;
            }

            bool canDash = _permanentModifiers.CanDash;
            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            if (!_isShowing
                && showRule.Kind == WorldTextHintShowKind.PlayerCanDashUnlocked
                && !_previousCanDash
                && canDash)
            {
                _runtimeEventSeen = true;
                TryShow();
            }

            _previousCanDash = canDash;
        }

        private void TryShow(bool forceManual = false)
        {
            if (_isShowing || definition == null)
            {
                return;
            }

            WorldTextHintShowRule showRule = GetEffectiveShowRule();

            if (!forceManual)
            {
                if (showRule.Kind == WorldTextHintShowKind.Manual)
                {
                    return;
                }

                if (showRule.RequireRuntimeEvent && !_runtimeEventSeen && showRule.Kind != WorldTextHintShowKind.OnEnable)
                {
                    return;
                }
            }

            if (!EnsureLocalPlayerRefs())
            {
                return;
            }

            if (GetEffectiveShowRule().Kind == WorldTextHintShowKind.PlayerCanDashUnlocked)
            {
                BindPermanentModifiersListener();
            }

            if (showRule.RequireLocalPlayerCanDash && (_permanentModifiers == null || !_permanentModifiers.CanDash))
            {
                return;
            }

            Transform anchor = ResolveCustomAnchor();

            if (_presenter.TryShowTriggerHint(this, definition, anchor))
            {
                _isShowing = true;
                _showStartedAt = Time.time;
            }
        }

        private void Hide()
        {
            if (!_isShowing)
            {
                return;
            }

            _presenter?.HideTriggerHint(this);
            _isShowing = false;
            _showStartedAt = -1f;
        }

        private void EvaluateHideRules()
        {
            WorldTextHintHideRule hideRule = GetEffectiveHideRule();

            switch (hideRule.Kind)
            {
                case WorldTextHintHideKind.InputAction:
                    if (_inputReader != null && WasInputPressed(hideRule.InputAction))
                    {
                        Hide();
                    }

                    break;

                case WorldTextHintHideKind.Timeout:
                    if (hideRule.TimeoutSeconds > 0f
                        && _showStartedAt >= 0f
                        && Time.time - _showStartedAt >= hideRule.TimeoutSeconds)
                    {
                        Hide();
                    }

                    break;
            }
        }

        private bool WasInputPressed(WorldTextHintInputKind inputKind)
        {
            if (_inputReader == null)
            {
                return false;
            }

            return inputKind switch
            {
                WorldTextHintInputKind.Dash => _inputReader.PeekDashPressedThisFrame(),
                WorldTextHintInputKind.Interact => _inputReader.PeekInteractPressedThisFrame(),
                WorldTextHintInputKind.Jump => _inputReader.PeekJumpPressedThisFrame(),
                _ => false,
            };
        }

        private bool EnsureLocalPlayerRefs()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null || !networkManager.IsClient || networkManager.LocalClient?.PlayerObject == null)
            {
                _presenter = null;
                _inputReader = null;
                _permanentModifiers = null;
                return false;
            }

            NetworkObject playerObject = networkManager.LocalClient.PlayerObject;

            if (_presenter == null || _presenter.NetworkObject != playerObject)
            {
                _presenter = playerObject.GetComponent<WorldTextHintPresenter>();
                _inputReader = playerObject.GetComponent<PlayerInputReader>();
                _permanentModifiers = playerObject.GetComponent<PlayerPermanentModifiers>();
            }

            return _presenter != null;
        }

        private Transform ResolveCustomAnchor()
        {
            if (anchorMarker != null)
            {
                return anchorMarker.Transform;
            }

            return customAnchor;
        }

        private WorldTextHintShowRule GetEffectiveShowRule()
        {
            if (definition != null && !useInlineOverrides)
            {
                return definition.ShowRule;
            }

            return inlineShowRule;
        }

        private WorldTextHintHideRule GetEffectiveHideRule()
        {
            if (definition != null && !useInlineOverrides)
            {
                return definition.HideRule;
            }

            return inlineHideRule;
        }
    }
}
