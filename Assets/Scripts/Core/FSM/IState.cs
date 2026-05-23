namespace Catsss.Core.FSM
{
    public interface IState
    {
        void OnEnter();
        void OnExit();
        void OnUpdate();
        void OnFixedUpdate();
    }
}
