using UnityEngine;

namespace Catsss.Configs.Charge
{
    /// <summary>Постоянное улучшение после завершения испытания пилона.</summary>
    [CreateAssetMenu(menuName = "Catsss/Charge/Permanent Modifier")]
    public sealed class PermanentModifierDefinition : ScriptableObject, IByteIdentifiable
    {
        [SerializeField, Range(1, 255)] private byte id = 1;
        [SerializeField] private string displayName = "Unlock Dash";
        [SerializeField] private PermanentModifierKind kind = PermanentModifierKind.UnlockDash;
        [Min(0.01f)] [SerializeField] private float value = 1f;

        public byte Id => id;
        public string DisplayName => displayName;
        public PermanentModifierKind Kind => kind;
        public float Value => value;
    }
}
