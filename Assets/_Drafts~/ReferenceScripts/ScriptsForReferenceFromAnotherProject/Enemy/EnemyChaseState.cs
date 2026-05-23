using UnityEngine;
using UnityEngine.AI;
public class EnemyChaseState : EnemyBaseState
{
    readonly NavMeshAgent navMeshAgent;
    readonly Transform target;
    public EnemyChaseState(Enemy enemy, Animator animator, NavMeshAgent navMeshAgent, Transform target) : base(enemy, animator)
    {
        this.navMeshAgent = navMeshAgent;
        this.target = target;
    }

    public override void OnEnter()
    {
        Debug.Log("Entering Chase State");
        animator.CrossFade(RunHash, CrossFadeDuration);
    }

    public override void OnUpdate()
    {
        navMeshAgent.SetDestination(target.position);
    }

    public override void OnFixedUpdate()
    {
        navMeshAgent.SetDestination(target.position);
    }
}
