using UnityEngine;

namespace Catsss.Configs.Charge
{
    /// <summary>Временный заряд: движение, таймер и правила метания/передачи.</summary>
    [CreateAssetMenu(menuName = "Catsss/Charge/Charge Type")]
    public sealed class ChargeTypeDefinition : ScriptableObject, IByteIdentifiable
    {
        [SerializeField, Range(1, 255)] private byte id = 1;
        [SerializeField] private string displayName = "Air Charge";

        [Header("Duration")]
        [Tooltip("0 = без лимита по времени (до финиша, сброса или штрафа).")]
        [Min(0f)] [SerializeField] private float durationSeconds = 60f;

        [Header("Throw / Conduit")]
        [Min(0f)]
        [Tooltip("Добавка к таймеру при возврате заряда после промаха. Игнорируется, если Duration = 0.")]
        [SerializeField] private float bonusReturnSecondsOnMiss = 1.5f;

        [Min(1)]
        [Tooltip("Промахов броска на одно испытание до командного штрафа.")]
        [SerializeField] private int maxThrowAttemptsPerTrial = 3;

        [Min(0f)]
        [Tooltip("Пауза перед повторным входом в Aim после поимки этого заряда (с).")]
        [SerializeField] private float throwCooldownAfterCatch = 0.4f;

        [Header("Movement")]
        [Min(0.1f)] [SerializeField] private float jumpHeightMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private bool suppressJump;
        [SerializeField] private bool isHeavy;
        [Min(0.1f)] [SerializeField] private float heavyGravityMultiplier = 2.5f;
        [SerializeField] private bool isAir;
        [Min(0.1f)] [SerializeField] private float airWindSpeedMultiplier = 2.5f;

        [Header("Visual (заглушки для будущего VFX)")]
        [SerializeField] private Color auraColor = Color.cyan;
        [SerializeField] private GameObject vfxPrefab;

        public byte Id => id;
        public string DisplayName => displayName;
        public float DurationSeconds => durationSeconds;
        public bool HasTimedDuration => durationSeconds > 0f;
        public float BonusReturnSecondsOnMiss => bonusReturnSecondsOnMiss;
        public int MaxThrowAttemptsPerTrial => maxThrowAttemptsPerTrial;
        public float ThrowCooldownAfterCatch => throwCooldownAfterCatch;
        public float JumpHeightMultiplier => jumpHeightMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public bool SuppressJump => suppressJump;
        public bool IsHeavy => isHeavy;
        public float HeavyGravityMultiplier => heavyGravityMultiplier;
        public bool IsAir => isAir;
        public float AirWindSpeedMultiplier => airWindSpeedMultiplier;
        public Color AuraColor => auraColor;
        public GameObject VfxPrefab => vfxPrefab;

        /// <summary>Оставшееся время после промаха. 0 = бессрочный заряд (таймер не запускается).</summary>
        public float GetRestoredDurationAfterMiss(float remainingSnapshot)
        {
            if (!HasTimedDuration)
            {
                return 0f;
            }

            return Mathf.Max(0f, remainingSnapshot) + bonusReturnSecondsOnMiss;
        }
    }
}
