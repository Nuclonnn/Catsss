
public class Transition : ITransition
{
    public IState To { get; private set; }
    public IPredicate Condition { get; private set; }

    public Transition(IState to, IPredicate condition)
    {
        To = to;
        Condition = condition;
    }
}
