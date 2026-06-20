using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и включает/выключает AeroZone (server-only).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AeroZone))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class AeroZoneSignalDriver : NetworkEmptyEventChannelSignalDriverBase
    {
        [Header("Target")]
        [SerializeField] private AeroZone zone;

        [Header("Actions")]
        [SerializeField] private AeroZoneSignalBinding onPressed;
        [SerializeField] private AeroZoneSignalBinding onReleased;
        [SerializeField] private AeroZoneSignalBinding onOneShot;

        private void Reset()
        {
            ResolveZoneReference();
        }

        private void Awake()
        {
            ResolveZoneReference();
        }

        protected override void HandlePressedServer()
        {
            ApplyBindingServer(onPressed);
        }

        protected override void HandleReleasedServer()
        {
            ApplyBindingServer(onReleased);
        }

        protected override void HandleOneShotServer()
        {
            ApplyBindingServer(onOneShot);
        }

        private void ApplyBindingServer(AeroZoneSignalBinding binding)
        {
            if (zone == null)
            {
                return;
            }

            bool zoneActive = ZoneSignalActionUtility.Apply(zone.IsZoneActive, binding.zoneAction);
            zone.SetRuntimeStateServer(zoneActive);
            SyncVisualStateClientRpc(zoneActive);
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

        private void ResolveZoneReference()
        {
            if (zone == null)
            {
                zone = GetComponent<AeroZone>();
            }
        }
    }
}
