using UnityEngine;
public abstract class BaseState : IState
{
    protected readonly PlayerController _playerController;
    protected readonly Animator _animator;

    protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
    protected static readonly int JumpHash = Animator.StringToHash("Jump");
    protected static readonly int DashHash = Animator.StringToHash("Dash");
    protected static readonly int AttackHash = Animator.StringToHash("Attack");

    protected const float crossFadeDuration = 0.1f;
    public BaseState(PlayerController playerController, Animator animator)
    {
        _playerController = playerController;
        _animator = animator;
    }
    public virtual void OnEnter() { }
    public virtual void OnExit() {}
    public virtual void OnUpdate() { }
    public virtual void OnFixedUpdate() { }
}

