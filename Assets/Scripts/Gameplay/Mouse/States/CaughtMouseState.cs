using Catsss.Core.FSM;

namespace Catsss.Gameplay.Mouse.States
{
    internal sealed class CaughtMouseState : IState
    {
        private readonly IMouseBrainStateHost _host;

        public CaughtMouseState(IMouseBrainStateHost host)
        {
            _host = host;
        }

        public void OnEnter()
        {
            _host.TransitionSignals.ClearCatchRequest();
            _host.SetDomeFleeActiveServer(false);
            _host.ShutdownNavMeshAgentServer();
            _host.NotifyCaughtServer();
        }

        public void OnExit() { }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }
    }
}
