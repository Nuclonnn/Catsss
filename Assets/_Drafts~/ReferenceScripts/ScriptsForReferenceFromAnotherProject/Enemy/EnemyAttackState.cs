using UnityEngine;
using UnityEngine.AI;

public class EnemyAttackState : EnemyBaseState
{
    readonly NavMeshAgent navMeshAgent;
    readonly Transform target;
    public EnemyAttackState(Enemy enemy, Animator animator, NavMeshAgent navMeshAgent, Transform target) : base(enemy, animator)
    {
        this.navMeshAgent = navMeshAgent;
        this.target = target;
    }

    public override void OnEnter()
    {
        Debug.Log("Entering Attack State");
        animator.CrossFade(AttackHash, CrossFadeDuration);
    }

    public override void OnUpdate(){
        navMeshAgent.SetDestination(target.position);
        enemy.Attack();
    }
}
