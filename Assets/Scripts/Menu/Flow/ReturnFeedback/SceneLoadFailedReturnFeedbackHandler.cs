namespace Catsss.Menu.Flow.ReturnFeedback
{
    /// <summary>
    /// SceneLoadFailed feedback обычно ставится до перехода в Returning (с именем сцены в context).
    /// Handler — no-op если context пустой (дублирующий вызов из flow).
    /// </summary>
    internal sealed class SceneLoadFailedReturnFeedbackHandler : IReturnFeedbackHandler
    {
        public MenuReturnReason Reason => MenuReturnReason.SceneLoadFailed;

        public void Apply(in MenuReturnFeedbackContext context)
        {
            if (string.IsNullOrWhiteSpace(context.SceneName))
            {
                return;
            }

            MenuReturnFeedback.SetPendingSceneLoadFailed(context.SceneName);
        }
    }
}
