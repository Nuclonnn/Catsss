using Catsss.Player;
using Catsss.Player.Aim;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges.Projectile
{
    /// <summary>
    /// Точка homing на игроке и поиск Transform цели по clientId (сервер + предпросмотр).
    /// </summary>
    public static class ProjectileHomingTarget
    {
        private const float DefaultChestHeight = 1.1f;

        /// <summary>Мировая точка доводки: грудь/центр ловли, не корень у ног.</summary>
        public static bool TryGetWorldPosition(Transform target, out Vector3 worldPosition)
        {
            worldPosition = default;

            if (target == null)
            {
                return false;
            }

            if (target.TryGetComponent(out PlayerThrowOrigin throwOrigin))
            {
                worldPosition = throwOrigin.WorldPosition;
                return true;
            }

            if (target.GetComponentInParent<NetworkPlayerController>() != null)
            {
                worldPosition = target.position + Vector3.up * DefaultChestHeight;
                return true;
            }

            worldPosition = target.position;
            return true;
        }

        public static Transform ResolveTransform(ulong clientId)
        {
            if (clientId == 0)
            {
                return null;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return null;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                if (client.ClientId != clientId || client.PlayerObject == null)
                {
                    continue;
                }

                return client.PlayerObject.transform;
            }

            return null;
        }
    }
}
