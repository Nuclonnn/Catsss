using System;

namespace Catsss.Menu
{
    /// <summary>
    /// Одноразовый «ящик»: MainMenu задаёт параметры следующего входа в геймплей-сцену,
    /// <see cref="GameplayNetworkSessionStarter"/> забирает их в первом кадре.
    /// Так UI не висит на ServiceLocator без NetworkManager.
    /// </summary>
    public static class NetworkSessionIntent
    {
        public const ushort DefaultPort = 7777;

        private static bool _hasLaunch;
        private static LaunchPayload _launch;

        public static void QueueHostLaunch(ushort port = DefaultPort, float holdLoadingOverlaySecondsAfterConnect = 0.5f)
        {
            _launch = LaunchPayload.ForHost(port, Math.Max(0f, holdLoadingOverlaySecondsAfterConnect));
            _hasLaunch = true;
        }

        public static void QueueClientLaunch(string hostAddressOrDns, ushort port = DefaultPort, float holdLoadingOverlaySecondsAfterConnect = 0.5f)
        {
            _launch = LaunchPayload.ForClient(hostAddressOrDns, port, Math.Max(0f, holdLoadingOverlaySecondsAfterConnect));
            _hasLaunch = true;
        }

        public static bool TryConsumeLaunch(out LaunchPayload payload)
        {
            if (!_hasLaunch)
            {
                payload = default;
                return false;
            }

            payload = _launch;
            _hasLaunch = false;
            _launch = default;
            return true;
        }

        public readonly struct LaunchPayload
        {
            public bool IsHost { get; }

            public ushort Port { get; }

            public string ClientRemoteHost { get; }

            public float HoldLoadingOverlaySecondsAfterConnect { get; }

            private LaunchPayload(bool isHost, ushort port, string remoteHost, float holdOverlay)
            {
                IsHost = isHost;
                Port = port;
                ClientRemoteHost = remoteHost ?? string.Empty;
                HoldLoadingOverlaySecondsAfterConnect = holdOverlay;
            }

            public static LaunchPayload ForHost(ushort port, float holdOverlay)
            {
                return new LaunchPayload(true, port, string.Empty, holdOverlay);
            }

            public static LaunchPayload ForClient(string hostAddress, ushort port, float holdOverlay)
            {
                return new LaunchPayload(false, port, hostAddress ?? string.Empty, holdOverlay);
            }
        }
    }
}
