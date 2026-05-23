using UnityEngine;

namespace Catsss.Core.WorldHints
{
    /// <summary>Опциональная метка точки крепления подсказки на объекте сцены.</summary>
    public sealed class WorldTextHintAnchor : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset;

        public Vector3 LocalOffset => localOffset;
        public Transform Transform => transform;
    }
}
