namespace Catsss.Menu.Flow.ReturnFeedback
{
    /// <summary>Применяет pending-feedback в <see cref="MenuReturnFeedback"/> для конкретной <see cref="MenuReturnReason"/>.</summary>
    internal interface IReturnFeedbackHandler
    {
        MenuReturnReason Reason { get; }

        void Apply(in MenuReturnFeedbackContext context);
    }
}
