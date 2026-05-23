using UnityEngine;

namespace Catsss.Configs.Charge
{
    /// <summary>Временный заряд: множители движения и ограничения на время испытания.</summary>
    [CreateAssetMenu(menuName = "Catsss/Charge/Charge Type")]
    public sealed class ChargeTypeDefinition : ScriptableObject, IByteIdentifiable
    {
        [SerializeField, Range(1, 255)] private byte id = 1;
        [SerializeField] private string displayName = "Air Charge";

        [Header("Duration")]
        [Tooltip("0 = без лимита по времени (до финиша, сброса или штрафа).")]
        [Min(0f)] [SerializeField] private float durationSeconds = 60f;

        [Header("Movement")]
        [Min(0.1f)] [SerializeField] private float jumpHeightMultiplier = 1f;
        [Min(0.1f)] [SerializeField] private float moveSpeedMultiplier = 1f;
        [SerializeField] private bool suppressJump;
        [SerializeField] private bool isHeavy;
        [Min(0.1f)] [SerializeField] private float heavyGravityMultiplier = 2.5f;

        [Header("Visual (заглушки для будущего VFX)")]
        [SerializeField] private Color auraColor = Color.cyan;
        [SerializeField] private GameObject vfxPrefab;

        public byte Id => id;
        public string DisplayName => displayName;
        public float DurationSeconds => durationSeconds;
        public float JumpHeightMultiplier => jumpHeightMultiplier;
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        public bool SuppressJump => suppressJump;
        public bool IsHeavy => isHeavy;
        public float HeavyGravityMultiplier => heavyGravityMultiplier;
        public Color AuraColor => auraColor;
        public GameObject VfxPrefab => vfxPrefab;
    }
}
