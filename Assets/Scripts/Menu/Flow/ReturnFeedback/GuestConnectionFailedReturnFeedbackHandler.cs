namespace Catsss.Menu.Flow.ReturnFeedback
{
    internal sealed class GuestConnectionFailedReturnFeedbackHandler : IReturnFeedbackHandler
    {
        public MenuReturnReason Reason => MenuReturnReason.GuestConnectionFailed;

        public void Apply(in MenuReturnFeedbackContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.GuestLastHost) || context.GuestLastPort > 0)
            {
                MenuReturnFeedback.SetPendingGuestConnectionError(context.GuestLastHost, context.GuestLastPort);
                return;
            }

            MenuReturnFeedback.SetPendingGuestConnectionError();
        }
    }
}
