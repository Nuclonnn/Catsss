namespace Catsss.Menu.Flow.ReturnFeedback
{
    internal sealed class SessionEndedReturnFeedbackHandler : IReturnFeedbackHandler
    {
        public MenuReturnReason Reason => MenuReturnReason.SessionEnded;

        public void Apply(in MenuReturnFeedbackContext context)
        {
            MenuReturnFeedback.SetPendingSessionEnded();
        }
    }
}
