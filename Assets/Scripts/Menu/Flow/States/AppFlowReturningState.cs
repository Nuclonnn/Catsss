namespace Catsss.Menu.Flow
{
    /// <summary>Очистка NGO и возврат в MainMenu.</summary>
    public sealed class AppFlowReturningState : AppFlowStateBase
    {
        public AppFlowReturningState(ApplicationFlowController flow) : base(flow)
        {
        }

        public override void OnEnter()
        {
            Flow.BeginReturnToMainMenuRoutine();
        }

        public override void OnExit()
        {
            Flow.CancelReturnToMainMenuRoutine();
        }
    }
}
