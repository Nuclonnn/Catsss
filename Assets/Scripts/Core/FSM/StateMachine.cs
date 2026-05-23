using System;
using System.Collections.Generic;

namespace Catsss.Core.FSM
{
    public sealed class StateMachine
    {
        private sealed class StateNode
        {
            public StateNode(IState state)
            {
                State = state ?? throw new ArgumentNullException(nameof(state));
            }

            public IState State { get; }
            public HashSet<ITransition> Transitions { get; } = new();
        }

        private readonly Dictionary<Type, StateNode> _nodes = new();
        private readonly HashSet<ITransition> _anyTransitions = new();
        private StateNode _currentState;

        public IState CurrentState => _currentState?.State;

        public void SetState(IState state)
        {
            _currentState?.State.OnExit();
            _currentState = GetOrAddNode(state);
            _currentState.State.OnEnter();
        }

        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).Transitions.Add(new Transition(GetOrAddNode(to).State, condition));
        }

        public void AddAnyTransition(IState to, IPredicate condition)
        {
            _anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
        }

        public void Update()
        {
            EnsureInitialized();
            ITransition transition = GetTransition();

            if (transition != null)
            {
                ChangeState(transition.To);
            }

            _currentState.State.OnUpdate();
        }

        public void FixedUpdate()
        {
            EnsureInitialized();
            _currentState.State.OnFixedUpdate();
        }

        private void ChangeState(IState state)
        {
            if (_currentState.State == state)
            {
                return;
            }

            _currentState.State.OnExit();
            _currentState = GetOrAddNode(state);
            _currentState.State.OnEnter();
        }

        private StateNode GetOrAddNode(IState state)
        {
            Type type = state.GetType();

            if (!_nodes.TryGetValue(type, out StateNode node))
            {
                node = new StateNode(state);
                _nodes.Add(type, node);
            }

            return node;
        }

        private ITransition GetTransition()
        {
            foreach (ITransition transition in _anyTransitions)
            {
                if (transition.Condition.Evaluate())
                {
                    return transition;
                }
            }

            foreach (ITransition transition in _currentState.Transitions)
            {
                if (transition.Condition.Evaluate())
                {
                    return transition;
                }
            }

            return null;
        }

        private void EnsureInitialized()
        {
            if (_currentState == null)
            {
                throw new InvalidOperationException("StateMachine has no current state. Call SetState before Update.");
            }
        }
    }
}
