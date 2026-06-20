using Catsss.Network;

namespace Catsss.Menu
{
    /// <summary>
    /// Статический «конверт» между геймплей-сценой и MainMenu после неудачного connect или обрыва сессии.
    /// Может нести снимок guest-формы (IP/порт), чтобы вернуть игрока на панель Connect с прежним вводом.
    /// </summary>
    public static class MenuReturnFeedback
    {
        private static bool _hasReturn;
        private static MenuReturnPayload _payload;

        public readonly struct MenuReturnPayload
        {
            public bool OpenGuestPanel { get; }

            public ClientConnectInputError GuestInputError { get; }

            public MenuReturnReason Reason { get; }

            public string Context { get; }

            /// <summary>IP, который гость вводил перед connect (если известен).</summary>
            public string GuestLastHost { get; }

            /// <summary>Порт guest connect (0 — не восстанавливать поле порта).</summary>
            public ushort GuestLastPort { get; }

            public bool ShouldShowSessionBanner =>
                Reason is MenuReturnReason.HostDisconnected
                    or MenuReturnReason.SessionEnded
                    or MenuReturnReason.SceneLoadFailed;

            public bool HasGuestFormSnapshot =>
                !string.IsNullOrWhiteSpace(GuestLastHost) || GuestLastPort > 0;

            public MenuReturnPayload(
                bool openGuestPanel,
                ClientConnectInputError guestInputError,
                MenuReturnReason reason,
                string context = null,
                string guestLastHost = null,
                ushort guestLastPort = 0)
            {
                OpenGuestPanel = openGuestPanel;
                GuestInputError = guestInputError;
                Reason = reason;
                Context = context ?? string.Empty;
                GuestLastHost = guestLastHost ?? string.Empty;
                GuestLastPort = guestLastPort;
            }
        }

        public static void SetPendingGuestConnectionError(string guestLastHost = null, ushort guestLastPort = 0)
        {
            _payload = new MenuReturnPayload(
                true,
                ClientConnectInputError.None,
                MenuReturnReason.GuestConnectionFailed,
                guestLastHost: guestLastHost,
                guestLastPort: guestLastPort);
            _hasReturn = true;
        }

        public static void SetPendingGuestInputError(
            ClientConnectInputError error,
            string guestLastHost = null,
            ushort guestLastPort = 0)
        {
            _payload = new MenuReturnPayload(
                true,
                error,
                MenuReturnReason.GuestInputError,
                guestLastHost: guestLastHost,
                guestLastPort: guestLastPort);
            _hasReturn = true;
        }

        public static void SetPendingHostDisconnected()
        {
            _payload = new MenuReturnPayload(false, ClientConnectInputError.None, MenuReturnReason.HostDisconnected);
            _hasReturn = true;
        }

        public static void SetPendingSessionEnded()
        {
            _payload = new MenuReturnPayload(false, ClientConnectInputError.None, MenuReturnReason.SessionEnded);
            _hasReturn = true;
        }

        public static void SetPendingSceneLoadFailed(string sceneName)
        {
            _payload = new MenuReturnPayload(
                false,
                ClientConnectInputError.None,
                MenuReturnReason.SceneLoadFailed,
                sceneName ?? string.Empty);
            _hasReturn = true;
        }

        public static bool TryConsumeReturn(out MenuReturnPayload payload)
        {
            payload = _payload;

            if (!_hasReturn)
            {
                payload = default;
                return false;
            }

            _hasReturn = false;
            _payload = default;
            return true;
        }
    }
}
