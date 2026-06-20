using System.Collections.Generic;
using Catsss.Core.Events;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>
    /// Trigger-зона: при входе игроков запускает сценический маршрут мыши на сервере.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MouseCueTrigger : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private MouseBrain mouse;
        [SerializeField] private MouseRoute route;

        [Header("Playback")]
        [SerializeField] private bool playOnce = true;
        [SerializeField] private bool teleportToRouteStart = true;
        [Tooltip("Если Play Once выключен — не запускать повторно, пока мышь ещё идёт по маршруту.")]
        [SerializeField] private bool blockWhileRouteActive = true;

        [Header("Players")]
        [SerializeField] private MouseCueCountRequirement countRequirement = MouseCueCountRequirement.AnyPlayerInZone;
        [SerializeField, Min(1)] private int minimumPlayersInZone = 1;

        [Header("Events")]
        [SerializeField] private EmptyEventChannel onStartedChannel;
        [SerializeField] private EmptyEventChannel onFinishedChannel;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color idleGizmoColor = new(0.95f, 0.55f, 0.15f, 0.25f);
        [SerializeField] private Color triggeredGizmoColor = new(0.95f, 0.2f, 0.2f, 0.35f);

        private readonly Dictionary<ulong, int> _overlapCountsByPlayer = new();
        private bool _hasTriggered;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerAuthority() || !TryResolvePlayer(other, out NetworkPlayerController player))
            {
                return;
            }

            ulong networkObjectId = player.NetworkObjectId;
            _overlapCountsByPlayer.TryGetValue(networkObjectId, out int currentCount);
            _overlapCountsByPlayer[networkObjectId] = currentCount + 1;
            TryTriggerCueServer();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServerAuthority() || !TryResolvePlayer(other, out NetworkPlayerController player))
            {
                return;
            }

            ulong networkObjectId = player.NetworkObjectId;

            if (!_overlapCountsByPlayer.TryGetValue(networkObjectId, out int currentCount))
            {
                return;
            }

            currentCount--;

            if (currentCount <= 0)
            {
                _overlapCountsByPlayer.Remove(networkObjectId);
            }
            else
            {
                _overlapCountsByPlayer[networkObjectId] = currentCount;
            }
        }

        private void TryTriggerCueServer()
        {
            if (!IsServerAuthority())
            {
                return;
            }

            if (mouse == null || route == null)
            {
                Debug.LogWarning($"[MouseCueTrigger] {name}: не назначены Mouse или Route.", this);
                return;
            }

            if (!route.IsValid(out string error))
            {
                Debug.LogWarning($"[MouseCueTrigger] {name}: маршрут невалиден: {error}", route);
                return;
            }

            if (playOnce && _hasTriggered)
            {
                return;
            }

            if (blockWhileRouteActive && mouse.IsFollowingRoute)
            {
                return;
            }

            if (!HasRequiredPlayerCount())
            {
                return;
            }

            if (playOnce)
            {
                _hasTriggered = true;
            }

            mouse.PlayRouteServer(
                route,
                teleportToRouteStart,
                onStartedChannel,
                onFinishedChannel);
        }

        private bool HasRequiredPlayerCount()
        {
            int playersInside = CountPlayersInside();

            return countRequirement switch
            {
                MouseCueCountRequirement.MinimumPlayers => playersInside >= minimumPlayersInZone,
                MouseCueCountRequirement.AllConnectedPlayers => playersInside >= CountConnectedPlayersWithPlayerObjects(),
                _ => playersInside > 0,
            };
        }

        private int CountPlayersInside()
        {
            int count = 0;

            foreach (KeyValuePair<ulong, int> pair in _overlapCountsByPlayer)
            {
                if (pair.Value > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountConnectedPlayersWithPlayerObjects()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return 0;
            }

            int count = 0;

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    count++;
                }
            }

            return Mathf.Max(1, count);
        }

        private static bool TryResolvePlayer(Collider other, out NetworkPlayerController player)
        {
            player = null;

            if (other == null)
            {
                return false;
            }

            NetworkPlayerController resolved = other.GetComponentInParent<NetworkPlayerController>();

            if (resolved == null || resolved.NetworkObject == null || !resolved.NetworkObject.IsSpawned)
            {
                return false;
            }

            player = resolved;
            return true;
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo)
            {
                return;
            }

            Collider col = GetComponent<Collider>();

            if (col == null)
            {
                return;
            }

            Gizmos.color = _hasTriggered ? triggeredGizmoColor : idleGizmoColor;
            Gizmos.matrix = col.transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
