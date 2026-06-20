namespace Catsss.Menu
{
    /// <summary>
    /// DTO параметров host/client сессии. Заполняется <see cref="Flow.ApplicationFlowController"/>.
    /// </summary>
    public static class NetworkSessionIntent
    {
        public const ushort DefaultPort = 7777;
        public const string DefaultLevelSceneName = "Sandbox";

        public readonly struct LaunchPayload
        {
            public bool IsHost { get; }

            public ushort Port { get; }

            public string ClientRemoteHost { get; }

            public string LevelSceneName { get; }

            public float HoldLoadingOverlaySecondsAfterConnect { get; }

            private LaunchPayload(bool isHost, ushort port, string remoteHost, string levelSceneName, float holdOverlay)
            {
                IsHost = isHost;
                Port = port;
                ClientRemoteHost = remoteHost ?? string.Empty;
                LevelSceneName = levelSceneName ?? DefaultLevelSceneName;
                HoldLoadingOverlaySecondsAfterConnect = holdOverlay;
            }

            public static LaunchPayload ForHost(ushort port, string levelSceneName, float holdOverlay)
            {
                return new LaunchPayload(true, port, string.Empty, levelSceneName, holdOverlay);
            }

            public static LaunchPayload ForClient(string hostAddress, ushort port, string levelSceneName, float holdOverlay)
            {
                return new LaunchPayload(false, port, hostAddress ?? string.Empty, levelSceneName, holdOverlay);
            }
        }
    }
}
