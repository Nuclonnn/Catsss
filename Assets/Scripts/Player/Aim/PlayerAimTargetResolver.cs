using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>
    /// Выбор цели для homing-броска: в duo — второй игрок; при &gt;2 — ближайший по дистанции.
    /// </summary>
    public static class PlayerAimTargetResolver
    {
        private static readonly List<NetworkPlayerController> Scratch = new(8);

        public static bool TryResolveTarget(NetworkPlayerController self, out NetworkPlayerController target)
        {
            target = null;

            if (self == null)
            {
                return false;
            }

            Scratch.Clear();
            CollectOtherPlayers(self, Scratch);

            if (Scratch.Count == 0)
            {
                return false;
            }

            if (Scratch.Count == 1)
            {
                target = Scratch[0];
                return true;
            }

            float bestSqr = float.MaxValue;
            Vector3 origin = self.transform.position;

            for (int i = 0; i < Scratch.Count; i++)
            {
                NetworkPlayerController candidate = Scratch[i];
                float sqr = (candidate.transform.position - origin).sqrMagnitude;

                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    target = candidate;
                }
            }

            return target != null;
        }

        private static void CollectOtherPlayers(NetworkPlayerController self, List<NetworkPlayerController> results)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null || !networkManager.IsClient)
            {
                return;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                NetworkObject playerObject = client.PlayerObject;

                if (playerObject == null
                    || !playerObject.TryGetComponent(out NetworkPlayerController controller)
                    || controller == self)
                {
                    continue;
                }

                results.Add(controller);
            }
        }
    }
}
