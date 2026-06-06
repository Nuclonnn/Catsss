using System.Collections.Generic;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Trigger-зона Resonance: держит платформу активной, пока на ней нужные игроки.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class KinematicPlatformResonanceZone : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private KinematicPlatform platform;

        [Header("Requirements")]
        [SerializeField] private KinematicPlatformResonanceRequirement requirement = KinematicPlatformResonanceRequirement.AnyPlayer;

        [Header("Release")]
        [Tooltip("Что делать с платформой, когда зона Resonance больше не удерживается.")]
        [SerializeField] private KinematicPlatformEndPolicy releasePolicy = KinematicPlatformEndPolicy.ReturnToStart;

        private readonly Dictionary<ulong, NetworkPlayerController> _playersInside = new();
        private readonly Dictionary<ulong, int> _overlapCountsByPlayer = new();
        private readonly List<ulong> _stalePlayers = new();
        private bool _isHeld;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            ResolvePlatformReference();
        }

        private void Awake()
        {
            ResolvePlatformReference();
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
            if (_isHeld && IsServerAuthority() && platform != null)
            {
                platform.SetResonanceHeldServer(false, releasePolicy);
            }

            _isHeld = false;
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
            if (platform == null)
            {
                ResolvePlatformReference();
            }

            if (platform == null)
            {
                return;
            }

            bool shouldHold = CountQualifiedPlayers() > 0;

            if (_isHeld == shouldHold)
            {
                return;
            }

            _isHeld = shouldHold;
            platform.SetResonanceHeldServer(shouldHold, releasePolicy);
        }

        private int CountQualifiedPlayers()
        {
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

            if (requirement == KinematicPlatformResonanceRequirement.AnyPlayer)
            {
                return true;
            }

            if (!player.TryGetComponent(out PlayerChargeController chargeController))
            {
                return false;
            }

            if (requirement == KinematicPlatformResonanceRequirement.AnyActiveCharge)
            {
                return chargeController.ChargeId != 0;
            }

            return chargeController.ActiveDefinition != null && chargeController.ActiveDefinition.IsHeavy;
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

        private void ResolvePlatformReference()
        {
            if (platform == null)
            {
                platform = GetComponentInParent<KinematicPlatform>();
            }
        }
    }
}
