namespace Catsss.Gameplay.Mouse.States
{
    internal sealed class MouseBrainStateSet
    {
        public MouseBrainStateSet(IMouseBrainStateHost host)
        {
            Hidden = new HiddenMouseState(host);
            RouteFollow = new RouteFollowMouseState(host);
            DomeFlee = new DomeFleeMouseState(host);
            Caught = new CaughtMouseState(host);
        }

        public HiddenMouseState Hidden { get; }

        public RouteFollowMouseState RouteFollow { get; }

        public DomeFleeMouseState DomeFlee { get; }

        public CaughtMouseState Caught { get; }
    }
}
