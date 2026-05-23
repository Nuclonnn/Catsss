using System;

public class FuncPredicate : IPredicate
{
    private readonly Func<bool> _predicate;

    public FuncPredicate(Func<bool> predicate)
    {
        _predicate = predicate;
    }

    public bool Evaluate()
    {
        return _predicate.Invoke();
    }
}
