using UnityEngine;

namespace Catsss.Configs.Charge
{
    /// <summary>Единая точка ссылок на каталоги контента уровня (назначается на сцене и игроке).</summary>
    [CreateAssetMenu(menuName = "Catsss/Charge/Gameplay Content Catalog")]
    public sealed class GameplayContentCatalog : ScriptableObject
    {
        [field: SerializeField] public ChargeTypeCatalog ChargeTypes { get; private set; }
        [field: SerializeField] public PermanentModifierCatalog PermanentModifiers { get; private set; }
    }
}
