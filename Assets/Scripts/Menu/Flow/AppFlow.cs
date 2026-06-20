namespace Catsss.Menu.Flow
{
    /// <summary>Удобный доступ к <see cref="IAppFlowCommands"/> из gameplay/network кода.</summary>
    public static class AppFlow
    {
        /// <summary>Bootstrap DontDestroyOnLoad flow, если ещё не создан.</summary>
        public static IAppFlowCommands EnsureExists() => ApplicationFlowController.EnsureExists();

        public static bool TryGet(out IAppFlowCommands commands) =>
            ApplicationFlowController.TryGet(out commands);
    }
}
