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

        private readonly CooldownTimer _durationTimer = new(0f);
        private ChargeTypeDefinition _cachedDefinition;

        public byte ChargeId => _chargeId.Value;
        public int ActiveTrialId => _activeTrialId.Value;
        public ChargeTypeDefinition ActiveDefinition => _cachedDefinition;

        public event Action<byte, int> ChargeChanged;

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

            if (definition.DurationSeconds > 0f)
            {
                _durationTimer.Restart(definition.DurationSeconds);
            }
            else
            {
                _durationTimer.Clear();
            }

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
