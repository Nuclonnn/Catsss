using Catsss.Network;

namespace Catsss.Menu.Flow.ReturnFeedback
{
    /// <summary>Контекст для handler'ов — guest form, input error, имя сцены.</summary>
    internal readonly struct MenuReturnFeedbackContext
    {
        public MenuReturnFeedbackContext(
            string guestLastHost = null,
            ushort guestLastPort = 0,
            string sceneName = null,
            ClientConnectInputError guestInputError = ClientConnectInputError.None)
        {
            GuestLastHost = guestLastHost ?? string.Empty;
            GuestLastPort = guestLastPort;
            SceneName = sceneName ?? string.Empty;
            GuestInputError = guestInputError;
        }

        public string GuestLastHost { get; }

        public ushort GuestLastPort { get; }

        public string SceneName { get; }

        public ClientConnectInputError GuestInputError { get; }

        public static MenuReturnFeedbackContext Empty => default;
    }
}
