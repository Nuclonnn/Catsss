using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>
    /// Точка вылета снаряда на префабе игрока. Позицию/ориентацию настраивают в Inspector.
    /// Направление броска в Aim — <see cref="PlayerThrowDirectionResolver"/> (луч из центра экрана), не rotation этого Transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerThrowOrigin : MonoBehaviour
    {
        [SerializeField] private Transform originTransform;

        /// <summary>Мировая позиция вылета (для ServerRpc и предпросмотра дуги).</summary>
        public Vector3 WorldPosition => ResolveTransform().position;

        /// <summary>Для отладки и будущих VFX «вылет из лап».</summary>
        public Transform OriginTransform => ResolveTransform();

        private void Reset()
        {
            originTransform = transform;
        }

        private Transform ResolveTransform() => originTransform != null ? originTransform : transform;
    }
}
