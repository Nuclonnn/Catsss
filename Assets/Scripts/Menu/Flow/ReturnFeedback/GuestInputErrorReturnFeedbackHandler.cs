using Catsss.Network;

namespace Catsss.Menu.Flow.ReturnFeedback
{
    internal sealed class GuestInputErrorReturnFeedbackHandler : IReturnFeedbackHandler
    {
        public MenuReturnReason Reason => MenuReturnReason.GuestInputError;

        public void Apply(in MenuReturnFeedbackContext context)
        {
            ClientConnectInputError error = context.GuestInputError == ClientConnectInputError.None
                ? ClientConnectInputError.InvalidHost
                : context.GuestInputError;

            MenuReturnFeedback.SetPendingGuestInputError(
                error,
                context.GuestLastHost,
                context.GuestLastPort);
        }
    }
}
