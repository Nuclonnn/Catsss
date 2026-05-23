using Catsss.Core.Localization;
using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>
    /// Переиспользуемый профиль подсказки: текст, якорь, приоритет, правила show/hide.
    /// Create → Catsss/World Hints/Hint Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "HintDefinition", menuName = "Catsss/World Hints/Hint Definition")]
    public sealed class WorldTextHintDefinition : ScriptableObject
    {
        [Header("Text")]
        [SerializeField] private LocalizedTextReference text = new();

        [Header("Placement")]
        [SerializeField] private WorldTextHintAnchorMode anchorMode = WorldTextHintAnchorMode.CustomTransform;
        [SerializeField] private Vector3 localOffset = new(0f, 1.4f, 0f);

        [Header("Priority")]
        [Tooltip("Больше = важнее. Взаимодействие (E) использует 10 по умолчанию.")]
        [SerializeField] private int priority = 100;

        [Header("Show")]
        [SerializeField] private WorldTextHintShowRule showRule;

        [Header("Hide")]
        [SerializeField] private WorldTextHintHideRule hideRule;

        public LocalizedTextReference Text => text;
        public WorldTextHintAnchorMode AnchorMode => anchorMode;
        public Vector3 LocalOffset => localOffset;
        public int Priority => priority;
        public WorldTextHintShowRule ShowRule => showRule;
        public WorldTextHintHideRule HideRule => hideRule;
    }
}
