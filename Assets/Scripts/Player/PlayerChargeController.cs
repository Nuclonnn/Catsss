using System;
using Catsss.Configs.Charge;
using Catsss.Core.Services;
using Catsss.Core.Timing;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>Серверный источник правды: активный заряд, trialId, таймер.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerChargeController : NetworkBehaviour
    {
        [SerializeField] private GameplayContentCatalog contentCatalog;

        private readonly NetworkVariable<byte> _chargeId = new();
        private readonly NetworkVariable<int> _activeTrialId = new(-1);
        private readonly NetworkVariable<float> _remainingDuration = new();

        private readonly CooldownTimer _durationTimer = new(0f);
        private ChargeTypeDefinition _cachedDefinition;

        public byte ChargeId => _chargeId.Value;
        public int ActiveTrialId => _activeTrialId.Value;
        public ChargeTypeDefinition ActiveDefinition => _cachedDefinition;

        /// <summary>Оставшееся время заряда (с); реплицируется с сервера для UI и Aim.</summary>
        public float RemainingDuration => _chargeId.Value == 0 ? 0f : _remainingDuration.Value;

        public bool HasActiveCharge => _chargeId.Value != 0;

        public event Action<byte, int> ChargeChanged;

        /// <summary>Сервер: снять заряд для полёта снаряда. Trial в реестре остаётся активным.</summary>
        public bool TryTakeChargeForThrowServer(out byte chargeId, out int trialId, out float remainingSnapshot)
        {
            chargeId = 0;
            trialId = -1;
            remainingSnapshot = 0f;

            if (!IsServer || _chargeId.Value == 0)
            {
                return false;
            }

            chargeId = _chargeId.Value;
            trialId = _activeTrialId.Value;
            remainingSnapshot = _durationTimer.IsRunning
                ? _durationTimer.Remaining
                : _remainingDuration.Value;

            _chargeId.Value = 0;
            _activeTrialId.Value = -1;
            _durationTimer.Clear();
            _remainingDuration.Value = 0f;
            return true;
        }

        /// <summary>Сервер: выдать заряд с произвольным оставшимся таймером (возврат после промаха, поимка — 4.3).</summary>
        public bool TryApplyChargeWithRemainingServer(byte chargeTypeId, int trialId, float remainingDuration)
        {
            if (!IsServer || chargeTypeId == 0 || _chargeId.Value != 0)
            {
                return false;
            }

            if (contentCatalog == null || contentCatalog.ChargeTypes == null
                || !contentCatalog.ChargeTypes.TryGet(chargeTypeId, out ChargeTypeDefinition definition))
            {
                Debug.LogWarning($"[PlayerChargeController] Неизвестный chargeTypeId={chargeTypeId}.", this);
                return false;
            }

            _chargeId.Value = chargeTypeId;
            _activeTrialId.Value = trialId;

            ApplyDurationFromDefinition(definition, remainingDuration);
            return true;
        }

        private void ApplyDurationFromDefinition(ChargeTypeDefinition definition, float requestedRemaining)
        {
            if (!definition.HasTimedDuration)
            {
                _durationTimer.Clear();
                _remainingDuration.Value = 0f;
                return;
            }

            float duration = requestedRemaining > 0f
                ? requestedRemaining
                : definition.DurationSeconds;

            if (duration > 0f)
            {
                _durationTimer.Restart(duration);
                _remainingDuration.Value = duration;
            }
            else
            {
                _durationTimer.Clear();
                _remainingDuration.Value = 0f;
            }
        }

        private void Awake()
        {
            _chargeId.OnValueChanged += OnChargeReplicated;
        }

        public override void OnDestroy()
        {
            _chargeId.OnValueChanged -= OnChargeReplicated;
        }

        public override void OnNetworkSpawn()
        {
            RefreshCachedDefinition(_chargeId.Value);
        }

        private void Update()
        {
            if (!IsServer || _chargeId.Value == 0)
            {
                return;
            }

            if (_durationTimer.IsRunning)
            {
                _durationTimer.Tick(Time.deltaTime);
                _remainingDuration.Value = _durationTimer.Remaining;

                if (_durationTimer.Remaining <= 0f)
                {
                    ApplyChargeTimerPenaltyServer();
                }
            }
        }

        public bool TryApplyChargeServer(byte chargeTypeId, int trialId)
        {
            if (!IsServer || chargeTypeId == 0)
            {
                return false;
            }

            if (_chargeId.Value != 0)
            {
                return false;
            }

            if (contentCatalog == null || contentCatalog.ChargeTypes == null
                || !contentCatalog.ChargeTypes.TryGet(chargeTypeId, out ChargeTypeDefinition definition))
            {
                Debug.LogWarning($"[PlayerChargeController] Неизвестный chargeTypeId={chargeTypeId}.", this);
                return false;
            }

            _chargeId.Value = chargeTypeId;
            _activeTrialId.Value = trialId;
            ApplyDurationFromDefinition(definition, definition.DurationSeconds);
            return true;
        }

        public void ClearChargeServer()
        {
            if (!IsServer)
            {
                return;
            }

            int previousTrialId = _activeTrialId.Value;
            ForceClearChargeServer();

            if (previousTrialId >= 0 && ServiceLocator.TryGet(out TrialSessionRegistry registry))
            {
                registry.CancelActiveTrial(previousTrialId);
            }
        }

        /// <summary>Сброс заряда без отмены trial (вызывается из реестра при командном штрафе).</summary>
        public void ForceClearChargeServer()
        {
            if (!IsServer)
            {
                return;
            }

            _chargeId.Value = 0;
            _activeTrialId.Value = -1;
            _durationTimer.Clear();
            _remainingDuration.Value = 0f;
        }

        private void ApplyChargeTimerPenaltyServer()
        {
            if (!IsServer)
            {
                return;
            }

            int trialId = _activeTrialId.Value;

            if (trialId >= 0 && ServiceLocator.TryGet(out TrialSessionRegistry registry))
            {
                registry.ApplyTeamTrialPenalty(trialId, TrialPenaltyReason.ChargeTimerExpired);
                return;
            }

            ForceClearChargeServer();
        }

        private void OnChargeReplicated(byte previous, byte current)
        {
            RefreshCachedDefinition(current);
            ChargeChanged?.Invoke(current, _activeTrialId.Value);
        }

        private void RefreshCachedDefinition(byte id)
        {
            _cachedDefinition = null;

            if (id == 0 || contentCatalog == null || contentCatalog.ChargeTypes == null)
            {
                return;
            }

            contentCatalog.ChargeTypes.TryGet(id, out _cachedDefinition);
        }
    }
}
