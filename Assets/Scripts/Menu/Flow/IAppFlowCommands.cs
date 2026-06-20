namespace Catsss.Menu.Flow
{
    /// <summary>
    /// Публичный контракт app-flow для gameplay/network слоёв без доступа к <see cref="ApplicationFlowController"/>.Instance.
    /// Реализация регистрируется в <see cref="Catsss.Core.Services.ServiceLocator"/> при bootstrap.
    /// </summary>
    public interface IAppFlowCommands
    {
        bool IsSessionStartupHandled { get; }

        bool IsInMainMenuFlow { get; }

        bool IsLoadingFlow { get; }

        bool IsInGameFlow { get; }

        bool IsReturningFlow { get; }

        void NotifyGameplaySessionActive();

        void NotifyMainMenuSceneLoaded();

        void RequestHostSession(ushort port, string levelSceneName, MenuLoadPresentation presentation);

        void RequestClientSession(string hostAddress, ushort port, string levelSceneName, MenuLoadPresentation presentation);

        void RequestReturnToMainMenu(MenuReturnReason reason, bool stopNetwork = true);
    }
}
