using Catsss.Menu;

namespace Catsss.Network
{
    /// <summary>
    /// Ожидаемое завершение сессии (ClientRpc от хоста до transport disconnect).
    /// Отличает «хост закрыл сессию» от «хост пропал».
    /// </summary>
    internal static class SessionEndSignal
    {
        private static bool _hasExpectedEnd;
        private static MenuReturnReason _expectedReason;

        public static void MarkExpectedRemoteEnd(MenuReturnReason reason)
        {
            _hasExpectedEnd = true;
            _expectedReason = reason;
        }

        public static bool TryConsumeExpectedRemoteEnd(out MenuReturnReason reason)
        {
            if (!_hasExpectedEnd)
            {
                reason = default;
                return false;
            }

            reason = _expectedReason;
            Clear();
            return true;
        }

        public static void Clear()
        {
            _hasExpectedEnd = false;
            _expectedReason = default;
        }
    }
}
