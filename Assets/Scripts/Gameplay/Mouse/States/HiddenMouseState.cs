using Catsss.Core.FSM;

namespace Catsss.Gameplay.Mouse.States
{
    internal sealed class HiddenMouseState : IState
    {
        private readonly IMouseBrainStateHost _host;
        private bool _teleportToHomeBurrow = true;

        public HiddenMouseState(IMouseBrainStateHost host)
        {
            _host = host;
        }

        public void Prepare(bool teleportToHomeBurrow)
        {
            _teleportToHomeBurrow = teleportToHomeBurrow;
        }

        public void OnEnter()
        {
            _host.TransitionSignals.ClearCatchRequest();
            bool teleportHome = _teleportToHomeBurrow && !_host.TransitionSignals.ShouldHiddenSkipHomeTeleport();
            _host.TransitionSignals.ClearPendingRouteEnd();
            _host.SetRouteFollowingServer(false);
            _host.SetDomeFleeActiveServer(false);
            _host.EnableNavMeshAgentServer(false);

            if (teleportHome)
            {
                _host.TeleportToHomeServer();
            }

            _host.SetPresenceModeServer(MousePresenceMode.Hidden);
        }

        public void OnExit() { }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }
    }
}
