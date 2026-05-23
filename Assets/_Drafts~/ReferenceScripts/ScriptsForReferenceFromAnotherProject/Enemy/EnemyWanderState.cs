using UnityEngine;
using UnityEngine.AI;

public class EnemyWanderState : EnemyBaseState
{
    private readonly NavMeshAgent navMeshAgent;
    private readonly float wanderRadius;
    private readonly Vector3 startPosition;
    private bool hasLoggedMissingNavMesh;

    public EnemyWanderState(Enemy enemy, Animator animator, NavMeshAgent navMeshAgent, float wanderRadius) : base(enemy, animator)
    {
        this.navMeshAgent = navMeshAgent;
        this.wanderRadius = wanderRadius;
        this.startPosition = enemy.transform.position;
    }

    public override void OnEnter()
    {
        Debug.Log("Entering Wander State");
        animator.CrossFade(WalkHash, CrossFadeDuration);
        SetRandomDestination();
    }

    public override void OnExit()
    {
        // Следующее состояние само отвечает за свою анимацию при входе.
    }
    public override void OnUpdate(){
        if (HasReachedDestination()){
            SetRandomDestination();
        }
    }

    private bool HasReachedDestination()
    {
        if (!CanMove())
        {
            return false;
        }

        return !navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance
        && (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f);
    }

    private void SetRandomDestination()
    {
        if (!CanMove())
        {
            return;
        }

        var randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += startPosition;

        // NavMesh.SamplePosition может не найти точку, если вокруг врага нет NavMesh.
        if (NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    private bool CanMove()
    {
        if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (!hasLoggedMissingNavMesh)
        {
            Debug.LogWarning($"{nameof(EnemyWanderState)} cannot move because {nameof(NavMeshAgent)} is not on a NavMesh.", enemy);
            hasLoggedMissingNavMesh = true;
        }

        return false;
    }
}
