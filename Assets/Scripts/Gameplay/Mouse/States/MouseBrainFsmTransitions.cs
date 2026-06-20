using Catsss.Core.FSM;

namespace Catsss.Gameplay.Mouse.States
{
    /// <summary>Declarative transitions mouse FSM (см. <see cref="StateMachine.AddTransition"/>).</summary>
    internal static class MouseBrainFsmTransitions
    {
        public static void Configure(
            StateMachine machine,
            MouseBrainStateSet states,
            MouseBrainTransitionSignals signals)
        {
            machine.AddTransition(
                states.DomeFlee,
                states.Caught,
                new FuncPredicate(() => signals.IsCatchRequested));

            machine.AddTransition(
                states.RouteFollow,
                states.Hidden,
                new FuncPredicate(() => signals.HasPendingRouteEnd(MouseRouteEndMode.ReturnHidden)));

            machine.AddTransition(
                states.RouteFollow,
                states.DomeFlee,
                new FuncPredicate(() => signals.HasPendingRouteEnd(MouseRouteEndMode.EnterDome)));
        }
    }
}
