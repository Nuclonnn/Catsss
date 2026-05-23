using UnityEngine;

namespace Catsss.Configs
{
    [CreateAssetMenu(menuName = "Catsss/Configs/Charge Type")]
    public sealed class ChargeType : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int Id { get; private set; } = 1;
        [field: SerializeField] public string DisplayName { get; private set; } = "Air";
        [field: SerializeField, Min(0f)] public float DurationTime { get; private set; } = 15f;
        [field: SerializeField, Min(0f)] public float JumpMultiplier { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float SpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField] public bool IsHeavy { get; private set; }
        [field: SerializeField] public GameObject VfxPrefab { get; private set; }//
        [field: SerializeField] public Color LightColor { get; private set; } = Color.cyan;
    }
}
