using Catsss.Gameplay.Charges.Projectile;
using Catsss.Configs.Charge;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>Лимит промахов броска на активное испытание.</summary>
    public sealed partial class TrialSessionRegistry
    {
        /// <summary>
        /// Сервер: учесть один промах. true = лимит исчерпан → штраф без возврата заряда.
        /// </summary>
        public bool TryRegisterThrowMissServer(int trialId)
        {
            if (!IsServer || trialId <= 0 || !_activeTrialIds.Contains(trialId))
            {
                return false;
            }

            byte chargeTypeId = ResolveChargeTypeIdForTrial(trialId);
            int maxAttempts = ResolveMaxThrowAttempts(chargeTypeId);

            if (!_attemptsLeftByTrial.ContainsKey(trialId))
            {
                InitializeThrowAttemptsForTrial(trialId, chargeTypeId);
            }

            int remainingAfterMiss = ConsumeThrowAttempt(trialId, maxAttempts);
            ProjectileThrowSignals.RaiseThrowAttemptsChanged(trialId, remainingAfterMiss, maxAttempts);

            if (enableThrowAttemptsDebugLogs)
            {
                Debug.Log(
                    $"[TrialSessionRegistry] Throw miss trialId={trialId}, attemptsLeft={remainingAfterMiss}/{maxAttempts}",
                    this);
            }

            return remainingAfterMiss <= 0;
        }

        public int GetThrowAttemptsRemaining(int trialId)
        {
            if (trialId <= 0)
            {
                return 0;
            }

            return _attemptsLeftByTrial.TryGetValue(trialId, out int left) ? left : 0;
        }

        private void InitializeThrowAttemptsForTrial(int trialId, byte chargeTypeId)
        {
            int maxAttempts = ResolveMaxThrowAttempts(chargeTypeId);
            _attemptsLeftByTrial[trialId] = maxAttempts;
            ProjectileThrowSignals.RaiseThrowAttemptsChanged(trialId, maxAttempts, maxAttempts);
        }

        private int ResolveMaxThrowAttempts(byte chargeTypeId)
        {
            if (chargeTypeId != 0
                && contentCatalog != null
                && contentCatalog.ChargeTypes != null
                && contentCatalog.ChargeTypes.TryGet(chargeTypeId, out ChargeTypeDefinition definition))
            {
                int configured = definition.MaxThrowAttemptsPerTrial;
                return configured > 0 ? configured : 3;
            }

            return 3;
        }

        private byte ResolveChargeTypeIdForTrial(int trialId)
        {
            TrialDefinition resolved = null;
            ForPylonsWithTrialId(trialId, pylon => resolved = pylon.TrialDefinition);

            return resolved != null ? resolved.ChargeTypeId : (byte)0;
        }

        private int ConsumeThrowAttempt(int trialId, int maxAttempts)
        {
            if (!_attemptsLeftByTrial.TryGetValue(trialId, out int left))
            {
                left = maxAttempts;
            }

            left = Mathf.Max(0, left - 1);
            _attemptsLeftByTrial[trialId] = left;
            return left;
        }

        private void ClearThrowAttemptsForTrial(int trialId)
        {
            _attemptsLeftByTrial.Remove(trialId);
        }
    }
}
