namespace Catsss.Menu.Flow
{
    /// <summary>Активная игровая сессия на уровне.</summary>
    public sealed class AppFlowInGameState : AppFlowStateBase
    {
        public AppFlowInGameState(ApplicationFlowController flow) : base(flow)
        {
        }

        public override void OnEnter()
        {
            Flow.EnableSessionGuard();
        }

        public override void OnExit()
        {
            Flow.DisableSessionGuard();
        }
    }
}
