using Catsss.Configs.Charge;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>
    /// Описание одного испытания: id, заряд при старте, награда при финише.
    /// Один и тот же ассет вешается на пилон и на зону финиша.
    /// </summary>
    [CreateAssetMenu(menuName = "Catsss/Trials/Trial Definition")]
    public sealed class TrialDefinition : ScriptableObject
    {
        [SerializeField] private int trialId = 1;
        [SerializeField] private ChargeTypeDefinition chargeType;
        [SerializeField] private PermanentModifierDefinition completionReward;

        public int TrialId => trialId;
        public ChargeTypeDefinition ChargeType => chargeType;
        public PermanentModifierDefinition CompletionReward => completionReward;

        public byte ChargeTypeId => chargeType != null ? chargeType.Id : (byte)0;
        public byte PermanentRewardId => completionReward != null ? completionReward.Id : (byte)0;

        public bool IsValid =>
            trialId > 0 && chargeType != null && chargeType.Id > 0
            && completionReward != null && completionReward.Id > 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (chargeType != null && chargeType.Id == 0)
            {
                Debug.LogWarning($"[{name}] ChargeType '{chargeType.name}' имеет Id=0.", this);
            }

            if (completionReward != null && completionReward.Id == 0)
            {
                Debug.LogWarning($"[{name}] CompletionReward '{completionReward.name}' имеет Id=0.", this);
            }
        }
#endif
    }
}
