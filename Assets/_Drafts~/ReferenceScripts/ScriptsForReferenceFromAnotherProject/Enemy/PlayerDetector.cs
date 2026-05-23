
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    [SerializeField] private float detectionAngle = 60f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float innerDetectionRadius = 4f;
    [SerializeField] private float detectionCooldown = 0.5f;
    [SerializeField] private float attackRange = 2f;

    public Health PlayerHealth { get; private set; }
    public Transform Player { get; private set; }
    private CooldownTimer detectionCooldownTimer;
    private IDetectionStrategy detectionStrategy;

    private void Awake()
    {
        detectionCooldownTimer = new CooldownTimer(detectionCooldown);
        var playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError($"{nameof(PlayerDetector)} requires an object with the Player tag in the scene.", this);
            return;
        }

        Player = playerObject.transform;
        detectionStrategy = new ConeDetectionStrategy(detectionAngle, detectionRadius, innerDetectionRadius);
        // Health может находиться как на корневом объекте игрока, так и на его дочернем объекте.
        PlayerHealth = Player.GetComponentInChildren<Health>();
    }

    private void Update()
    {
        detectionCooldownTimer?.Tick(Time.deltaTime);
    }

    public bool CanDetectPlayer()
    {
        if (Player == null || detectionStrategy == null || detectionCooldownTimer == null)
        {
            return false;
        }

        return detectionCooldownTimer.IsRunning || detectionStrategy.Execute(Player, transform, detectionCooldownTimer);
    }

    public bool CanAttackPlayer()
    {
        if (Player == null || PlayerHealth == null)
        {
            return false;
        }
        
        return Vector3.Distance(transform.position, Player.position) <= attackRange;
    }

    public void SetDetectionStrategy(IDetectionStrategy detectionStrategy)
    {
        this.detectionStrategy = detectionStrategy;
    }

    void OnDrawGizmosSelected()
    {
        if (Player == null || detectionStrategy == null || detectionCooldownTimer == null)
        {
            return;
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, innerDetectionRadius);
        
        Vector3 forwardConeDirection = Quaternion.Euler(0f, detectionAngle / 2f, 0f) * transform.forward * detectionRadius;
        Vector3 backwardConeDirection = Quaternion.Euler(0f, -detectionAngle / 2f, 0f) * transform.forward * detectionRadius;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + forwardConeDirection);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + backwardConeDirection);

    }

}
