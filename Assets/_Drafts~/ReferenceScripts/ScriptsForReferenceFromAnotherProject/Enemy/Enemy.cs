using UnityEngine;
using UnityEngine.AI;
using KBCore.Refs;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerDetector))]
public class Enemy : Entity
{
    [SerializeField, Self] private NavMeshAgent navMeshAgent;
    [SerializeField, Self] private PlayerDetector playerDetector;
    [SerializeField, Child] private Animator animator;
    [SerializeField, Min(0f)] private float wanderRadius = 20f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;

    private StateMachine stateMachine;
    private CooldownTimer attackTimer;

    private void OnValidate()
    {
        this.ValidateRefs();
        ResolveRequiredComponents();
    }

    private void Awake()
    {
        ResolveRequiredComponents();
        attackTimer = new CooldownTimer(attackCooldown);
    }

    private void Start()
    {
        if (!CanInitialize())
        {
            enabled = false;
            return;
        }

        stateMachine = new StateMachine();

        var wanderState = new EnemyWanderState(this, animator, navMeshAgent, wanderRadius);
        var chaseState = new EnemyChaseState(this, animator, navMeshAgent, playerDetector.Player);
        var attackState = new EnemyAttackState(this, animator, navMeshAgent, playerDetector.Player);

        
        At(wanderState, chaseState, new FuncPredicate(() => playerDetector.CanDetectPlayer()));
        At(chaseState, wanderState, new FuncPredicate(() => !playerDetector.CanDetectPlayer()));
        At(chaseState, attackState, new FuncPredicate(() => playerDetector.CanAttackPlayer()));
        At(attackState, wanderState, new FuncPredicate(() => !playerDetector.CanAttackPlayer()));

        stateMachine.SetState(wanderState);
    }

    void At(IState from, IState to, IPredicate condition)=>stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition)=>stateMachine.AddAnyTransition(to, condition);

    private void Update()
    {
        stateMachine?.Update();
    }

    private void FixedUpdate()
    {
        stateMachine?.FixedUpdate();
        attackTimer?.Tick(Time.deltaTime);
    }

    private void ResolveRequiredComponents()
    {
        // Поля могут не заполниться атрибутами в runtime, поэтому дублируем поиск явно.
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (playerDetector == null)
        {
            playerDetector = GetComponent<PlayerDetector>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private bool CanInitialize()
    {
        if (navMeshAgent == null)
        {
            Debug.LogError($"{nameof(Enemy)} requires a {nameof(NavMeshAgent)} component.", this);
            return false;
        }

        if (playerDetector == null)
        {
            Debug.LogError($"{nameof(Enemy)} requires a {nameof(PlayerDetector)} component.", this);
            return false;
        }

        if (playerDetector.Player == null)
        {
            Debug.LogError($"{nameof(Enemy)} cannot chase because {nameof(PlayerDetector)} did not find the player.", this);
            return false;
        }

        if (playerDetector.PlayerHealth == null)
        {
            Debug.LogError($"{nameof(Enemy)} cannot attack because the player object does not have a {nameof(Health)} component.", this);
            return false;
        }

        if (animator == null)
        {
            Debug.LogError($"{nameof(Enemy)} requires an {nameof(Animator)} on itself or a child object.", this);
            return false;
        }

        return true;
    }

    public void Attack(){
        if (attackTimer.IsRunning) return;
        
        attackTimer.Start();
        playerDetector.PlayerHealth.TakeDamage((int)attackDamage);

    }
}
