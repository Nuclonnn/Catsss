using System.Collections.Generic;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Trigger-источник для MagicSeal: нажимные плиты, зоны касания и печати,
    /// которым нужен обычный игрок, тяжёлый заряд или несколько игроков сразу.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MagicSealTriggerActivator : MonoBehaviour
    {
        private static int _nextSourceId;

        [Header("Target")]
        [SerializeField] private MagicSeal seal;

        [Header("Requirements")]
        [SerializeField] private MagicSealPlayerRequirement playerRequirement = MagicSealPlayerRequirement.AnyPlayer;
        [SerializeField] private MagicSealCountRequirement countRequirement = MagicSealCountRequirement.AnyQualifiedPlayer;
        [SerializeField, Min(1)] private int minimumQualifiedPlayers = 2;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color idleGizmoColor = new(0.2f, 0.8f, 1f, 0.25f);
        [SerializeField] private Color activeGizmoColor = new(0.1f, 1f, 0.35f, 0.35f);

        private readonly Dictionary<ulong, NetworkPlayerController> _playersInside = new();
        private readonly Dictionary<ulong, int> _overlapCountsByPlayer = new();
        private readonly List<ulong> _stalePlayers = new();
        private int _sourceId;
        private bool _isHoldingSeal;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            ResolveSealReference();
        }

        private void Awake()
        {
            _sourceId = ++_nextSourceId;
            ResolveSealReference();
        }

        private void FixedUpdate()
        {
            if (!IsServerAuthority())
            {
                return;
            }

            EvaluateHoldState();
        }

        private void OnDisable()
        {
            if (_isHoldingSeal && IsServerAuthority() && seal != null)
            {
                seal.SetHeldBySourceServer(_sourceId, false, null);
            }

            _isHoldingSeal = false;
            _playersInside.Clear();
            _overlapCountsByPlayer.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerAuthority() || !TryResolvePlayer(other, out NetworkPlayerController player))
            {
                return;
            }

            ulong networkObjectId = player.NetworkObject.NetworkObjectId;
            _playersInside[networkObjectId] = player;
            _overlapCountsByPlayer.TryGetValue(networkObjectId, out int currentCount);
            _overlapCountsByPlayer[networkObjectId] = currentCount + 1;
            EvaluateHoldState();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServerAuthority() || !TryResolvePlayer(other, out NetworkPlayerController player))
            {
                return;
            }

            ulong networkObjectId = player.NetworkObject.NetworkObjectId;

            if (!_overlapCountsByPlayer.TryGetValue(networkObjectId, out int currentCount))
            {
                return;
            }

            currentCount--;

            if (currentCount <= 0)
            {
                _overlapCountsByPlayer.Remove(networkObjectId);
                _playersInside.Remove(networkObjectId);
            }
            else
            {
                _overlapCountsByPlayer[networkObjectId] = currentCount;
            }

            EvaluateHoldState();
        }

        private void EvaluateHoldState()
        {
            if (seal == null)
            {
                ResolveSealReference();
            }

            if (seal == null)
            {
                return;
            }

            int qualifiedCount = CountQualifiedPlayers(out NetworkPlayerController firstQualifiedPlayer);
            bool shouldHold = HasRequiredCount(qualifiedCount);

            if (_isHoldingSeal == shouldHold)
            {
                return;
            }

            _isHoldingSeal = shouldHold;
            seal.SetHeldBySourceServer(_sourceId, shouldHold, firstQualifiedPlayer);
        }

        private int CountQualifiedPlayers(out NetworkPlayerController firstQualifiedPlayer)
        {
            firstQualifiedPlayer = null;
            _stalePlayers.Clear();
            int count = 0;

            foreach (KeyValuePair<ulong, NetworkPlayerController> pair in _playersInside)
            {
                NetworkPlayerController player = pair.Value;

                if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned)
                {
                    _stalePlayers.Add(pair.Key);
                    continue;
                }

                if (!IsQualified(player))
                {
                    continue;
                }

                firstQualifiedPlayer ??= player;
                count++;
            }

            for (int i = 0; i < _stalePlayers.Count; i++)
            {
                ulong id = _stalePlayers[i];
                _playersInside.Remove(id);
                _overlapCountsByPlayer.Remove(id);
            }

            return count;
        }

        private bool IsQualified(NetworkPlayerController player)
        {
            if (player == null)
            {
                return false;
            }

            if (playerRequirement == MagicSealPlayerRequirement.AnyPlayer)
            {
                return true;
            }

            return player.TryGetComponent(out PlayerChargeController chargeController)
                   && chargeController.ActiveDefinition != null
                   && chargeController.ActiveDefinition.IsHeavy;
        }

        private bool HasRequiredCount(int qualifiedCount)
        {
            return countRequirement switch
            {
                MagicSealCountRequirement.MinimumQualifiedPlayers => qualifiedCount >= minimumQualifiedPlayers,
                MagicSealCountRequirement.AllConnectedPlayers => qualifiedCount >= CountConnectedPlayersWithPlayerObjects(),
                _ => qualifiedCount > 0,
            };
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

        private void ResolveSealReference()
        {
            if (seal == null)
            {
                seal = GetComponentInParent<MagicSeal>();
            }
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

            Gizmos.color = _isHoldingSeal ? activeGizmoColor : idleGizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider box:
                    Gizmos.DrawCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    break;
                case CapsuleCollider capsule:
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                    break;
            }
        }
    }
}
