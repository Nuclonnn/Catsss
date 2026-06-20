using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и переключает AntiMagicZone / BlocksProjectiles (server-only).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AntiMagicZone))]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class AntiMagicZoneSignalDriver : NetworkEmptyEventChannelSignalDriverBase
    {
        [Header("Target")]
        [SerializeField] private AntiMagicZone zone;

        [Header("Actions")]
        [SerializeField] private AntiMagicZoneSignalBinding onPressed;
        [SerializeField] private AntiMagicZoneSignalBinding onReleased;
        [SerializeField] private AntiMagicZoneSignalBinding onOneShot;

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

        private void ApplyBindingServer(AntiMagicZoneSignalBinding binding)
        {
            if (zone == null)
            {
                return;
            }

            bool zoneActive = ZoneSignalActionUtility.Apply(zone.IsZoneActive, binding.zoneAction);
            bool blocksProjectiles = ZoneSignalActionUtility.Apply(zone.BlocksProjectiles, binding.projectileBlockAction);

            zone.SetRuntimeStateServer(zoneActive, blocksProjectiles);
            SyncVisualStateClientRpc(zoneActive, blocksProjectiles);
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

        private void ResolveZoneReference()
        {
            if (zone == null)
            {
                zone = GetComponent<AntiMagicZone>();
            }
        }
    }
}
