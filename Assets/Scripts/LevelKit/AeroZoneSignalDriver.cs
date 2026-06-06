using Catsss.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и включает/выключает AeroZone (server-only).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AeroZone))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class AeroZoneSignalDriver : NetworkBehaviour
    {
        [Header("Target")]
        [SerializeField] private AeroZone zone;

        [Header("Channels")]
        [SerializeField] private EmptyEventChannel pressedChannel;
        [SerializeField] private EmptyEventChannel releasedChannel;
        [SerializeField] private EmptyEventChannel oneShotChannel;

        [Header("Actions")]
        [SerializeField] private AeroZoneSignalBinding onPressed;
        [SerializeField] private AeroZoneSignalBinding onReleased;
        [SerializeField] private AeroZoneSignalBinding onOneShot;

        private void Reset()
        {
            if (zone == null)
            {
                zone = GetComponent<AeroZone>();
            }
        }

        private void Awake()
        {
            if (zone == null)
            {
                zone = GetComponent<AeroZone>();
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

        private void ApplyBindingServer(AeroZoneSignalBinding binding)
        {
            if (!IsServerAuthority() || zone == null)
            {
                return;
            }

            bool zoneActive = ApplyAction(zone.IsZoneActive, binding.zoneAction);
            zone.SetRuntimeStateServer(zoneActive);
            SyncVisualStateClientRpc(zoneActive);
        }

        private static bool ApplyAction(bool current, AeroZoneSignalAction action)
        {
            return action switch
            {
                AeroZoneSignalAction.Enable => true,
                AeroZoneSignalAction.Disable => false,
                AeroZoneSignalAction.Toggle => !current,
                _ => current,
            };
        }

        [ClientRpc]
        private void SyncVisualStateClientRpc(bool zoneActive)
        {
            if (zone == null)
            {
                return;
            }

            zone.ApplyVisualStateClient(zoneActive);
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }
    }
}
