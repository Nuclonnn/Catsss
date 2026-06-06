using Catsss.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и переключает AntiMagicZone / BlocksProjectiles (server-only).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AntiMagicZone))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class AntiMagicZoneSignalDriver : NetworkBehaviour
    {
        [Header("Target")]
        [SerializeField] private AntiMagicZone zone;

        [Header("Channels")]
        [SerializeField] private EmptyEventChannel pressedChannel;
        [SerializeField] private EmptyEventChannel releasedChannel;
        [SerializeField] private EmptyEventChannel oneShotChannel;

        [Header("Actions")]
        [SerializeField] private AntiMagicZoneSignalBinding onPressed;
        [SerializeField] private AntiMagicZoneSignalBinding onReleased;
        [SerializeField] private AntiMagicZoneSignalBinding onOneShot;

        private void Reset()
        {
            if (zone == null)
            {
                zone = GetComponent<AntiMagicZone>();
            }
        }

        private void Awake()
        {
            if (zone == null)
            {
                zone = GetComponent<AntiMagicZone>();
            }
        }

        private void OnEnable()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised += OnPressed;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised += OnReleased;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised += OnOneShot;
            }
        }

        private void OnDisable()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised -= OnPressed;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised -= OnReleased;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised -= OnOneShot;
            }
        }

        private void OnPressed(EmptyEvent _)
        {
            ApplyBindingServer(onPressed);
        }

        private void OnReleased(EmptyEvent _)
        {
            ApplyBindingServer(onReleased);
        }

        private void OnOneShot(EmptyEvent _)
        {
            ApplyBindingServer(onOneShot);
        }

        private void ApplyBindingServer(AntiMagicZoneSignalBinding binding)
        {
            if (!IsServerAuthority() || zone == null)
            {
                return;
            }

            bool zoneActive = ApplyAction(zone.IsZoneActive, binding.zoneAction);
            bool blocksProjectiles = ApplyAction(zone.BlocksProjectiles, binding.projectileBlockAction);

            zone.SetRuntimeStateServer(zoneActive, blocksProjectiles);
            SyncVisualStateClientRpc(zoneActive, blocksProjectiles);
        }

        private static bool ApplyAction(bool current, AntiMagicZoneSignalAction action)
        {
            return action switch
            {
                AntiMagicZoneSignalAction.Enable => true,
                AntiMagicZoneSignalAction.Disable => false,
                AntiMagicZoneSignalAction.Toggle => !current,
                _ => current,
            };
        }

        [ClientRpc]
        private void SyncVisualStateClientRpc(bool zoneActive, bool blocksProjectiles)
        {
            if (zone == null)
            {
                return;
            }

            zone.ApplyVisualStateClient(zoneActive, blocksProjectiles);
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }
    }
}
