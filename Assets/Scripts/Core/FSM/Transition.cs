using System;

namespace Catsss.Core.FSM
{
    public sealed class Transition : ITransition
    {
        public Transition(IState to, IPredicate condition)
        {
            To = to ?? throw new ArgumentNullException(nameof(to));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public IState To { get; }
        public IPredicate Condition { get; }
    }
}
