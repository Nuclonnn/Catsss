using UnityEngine;

public abstract class EnemyBaseState : IState
{
    protected readonly Enemy enemy;
    protected readonly Animator animator;

    //protected static readonly int IdleHash = Animator.StringToHash("Idle");
    protected static readonly int WalkHash = Animator.StringToHash("Walk");
    protected static readonly int RunHash = Animator.StringToHash("Run");
    protected static readonly int AttackHash = Animator.StringToHash("Attack");
    protected static readonly int DeathHash = Animator.StringToHash("Death");
    
    protected const float CrossFadeDuration = 0.1f;

    protected EnemyBaseState(Enemy enemy, Animator animator){
        this.enemy = enemy;
        this.animator = animator;
    }
    public virtual void OnEnter()
    {
        //
    }

    public virtual void OnExit()
    {
        //
    }
    public virtual void OnUpdate()
    {
        //
    }
    public virtual void OnFixedUpdate()
    {
        //
    }
}
