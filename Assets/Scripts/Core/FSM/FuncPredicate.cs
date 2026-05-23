using System;

namespace Catsss.Core.FSM
{
    public sealed class FuncPredicate : IPredicate
    {
        private readonly Func<bool> _predicate;

        public FuncPredicate(Func<bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public bool Evaluate()
        {
            return _predicate.Invoke();
        }
    }
}
