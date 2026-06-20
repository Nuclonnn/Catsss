namespace Catsss.Menu.Flow
{
    /// <summary>Загрузка сцены уровня и NGO connect.</summary>
    public sealed class AppFlowLoadingState : AppFlowStateBase
    {
        public AppFlowLoadingState(ApplicationFlowController flow) : base(flow)
        {
        }

        public override void OnEnter()
        {
            Flow.BeginSessionLoadRoutine();
        }

        public override void OnExit()
        {
            Flow.CancelSessionLoadRoutine();
        }
    }
}
