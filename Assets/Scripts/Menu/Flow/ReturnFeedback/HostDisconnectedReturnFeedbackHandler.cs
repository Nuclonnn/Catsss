namespace Catsss.Menu.Flow.ReturnFeedback
{
    internal sealed class HostDisconnectedReturnFeedbackHandler : IReturnFeedbackHandler
    {
        public MenuReturnReason Reason => MenuReturnReason.HostDisconnected;

        public void Apply(in MenuReturnFeedbackContext context)
        {
            MenuReturnFeedback.SetPendingHostDisconnected();
        }
    }
}
