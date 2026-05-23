using UnityEngine;

namespace Catsss.Configs
{
    [CreateAssetMenu(menuName = "Catsss/Configs/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [field: SerializeField] public PlayerMovementSettings PlayerMovement { get; private set; } = new();
        [field: SerializeField] public PhysicsSettings Physics { get; private set; } = new();
    }
}
