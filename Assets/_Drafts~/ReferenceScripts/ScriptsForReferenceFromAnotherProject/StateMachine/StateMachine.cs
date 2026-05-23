using UnityEngine;
using System.Collections.Generic;
using System;

public class StateMachine
{
    class StateNode{
        public IState State{get;private set;}
        public HashSet<ITransition> Transitions{get;private set;}

        public StateNode(IState state)
        {
            State = state;
            Transitions = new HashSet<ITransition>();
        }
        public void AddTransition(IState to, IPredicate condition)
        {
            Transitions.Add(new Transition(to, condition));
        }
    }

    StateNode _currentState;
    Dictionary<Type, StateNode> _nodes = new();
    HashSet<ITransition> _anyTransitions = new();

    public void Update(){
        var transition = GetTransition();
        if(transition != null){
            ChangeState(transition.To);
        }
        _currentState.State.OnUpdate();
    }

    public void FixedUpdate(){
        _currentState.State.OnFixedUpdate();
    }

    public void SetState(IState state){
        _currentState = _nodes[state.GetType()];
        _currentState.State?.OnEnter();
    }
    
    public void AddTransition(IState from, IState to, IPredicate condition){
        GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
    }

    public void AddAnyTransition(IState to, IPredicate condition){
        _anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    void ChangeState(IState state){
        if(_currentState.State == state){
            return;
        }
        var previousState = _currentState;
        var nextState = _nodes[state.GetType()].State;

        previousState?.State?.OnExit();
        nextState?.OnEnter();
        _currentState = _nodes[state.GetType()];
    }

    StateNode GetOrAddNode(IState state){
        var node = _nodes.GetValueOrDefault(state.GetType());
        if(node == null){
            node = new StateNode(state);
            _nodes.Add(state.GetType(), node);
        }
        return node;
    }

    ITransition GetTransition(){
        foreach(var transition in _anyTransitions){
            if(transition.Condition.Evaluate()){
                return transition;
            }
        }
        foreach(var transition in _currentState.Transitions){
            if(transition.Condition.Evaluate()){
                return transition;
            }
        }
        return null;
    }
}
