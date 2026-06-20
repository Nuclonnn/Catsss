namespace Catsss.Gameplay.Mouse.States
{
    /// <summary>Флаги для declarative transitions в <see cref="MouseBrainFsmTransitions"/>.</summary>
    internal sealed class MouseBrainTransitionSignals
    {
        private bool _catchRequested;
        private MouseRouteEndMode? _pendingRouteEnd;
        private bool _routeEndHiddenAtCurrentPosition;

        public void RequestCatch()
        {
            _catchRequested = true;
        }

        public bool IsCatchRequested => _catchRequested;

        public void ClearCatchRequest()
        {
            _catchRequested = false;
        }

        public void SetPendingRouteEnd(MouseRouteEndMode endMode, bool hiddenAtCurrentPosition = false)
        {
            _pendingRouteEnd = endMode;
            _routeEndHiddenAtCurrentPosition = hiddenAtCurrentPosition;
        }

        public bool HasPendingRouteEnd(MouseRouteEndMode endMode)
        {
            return _pendingRouteEnd == endMode;
        }

        public bool ShouldHiddenSkipHomeTeleport()
        {
            return _pendingRouteEnd == MouseRouteEndMode.ReturnHidden && _routeEndHiddenAtCurrentPosition;
        }

        public void ClearPendingRouteEnd()
        {
            _pendingRouteEnd = null;
            _routeEndHiddenAtCurrentPosition = false;
        }
    }
}
