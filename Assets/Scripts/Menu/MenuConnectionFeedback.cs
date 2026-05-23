using Catsss.Network;

namespace Catsss.Menu
{
    /// <summary>
    /// Статический «конверт» между геймплей-сценой и MainMenu после неудачного client connect.
    /// </summary>
    public static class MenuConnectionFeedback
    {
        private static bool _openGuestPanel;
        private static ClientConnectInputError _returnError;

        public static void SetPendingGuestConnectionError()
        {
            _openGuestPanel = true;
            _returnError = ClientConnectInputError.None;
        }

        public static void SetPendingGuestInputError(ClientConnectInputError error)
        {
            _openGuestPanel = true;
            _returnError = error;
        }

        /// <summary>
        /// True — нужно показать ошибку на GuestPanel.
        /// returnError == None означает ошибку сетевого подключения (timeout / transport).
        /// </summary>
        public static bool TryConsumeGuestReturn(out bool openGuestPanel, out ClientConnectInputError returnError)
        {
            openGuestPanel = _openGuestPanel;
            returnError = _returnError;

            if (!_openGuestPanel && _returnError == ClientConnectInputError.None)
            {
                return false;
            }

            _openGuestPanel = false;
            _returnError = ClientConnectInputError.None;
            return true;
        }
    }
}
