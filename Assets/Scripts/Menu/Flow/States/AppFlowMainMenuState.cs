namespace Catsss.Menu.Flow
{
    /// <summary>Игрок в MainMenu — UI управляет панелями, flow ждёт запроса на сессию.</summary>
    public sealed class AppFlowMainMenuState : AppFlowStateBase
    {
        public AppFlowMainMenuState(ApplicationFlowController flow) : base(flow)
        {
        }
    }
}
