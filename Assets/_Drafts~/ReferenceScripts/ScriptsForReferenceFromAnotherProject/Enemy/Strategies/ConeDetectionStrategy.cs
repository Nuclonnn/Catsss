using UnityEngine;

public class ConeDetectionStrategy : IDetectionStrategy
{
    private readonly float detectionAngle;
    private readonly float detectionRadius;
    private readonly float innerDetectionRadius;

    public ConeDetectionStrategy(float detectionAngle, float detectionRadius, float innerDetectionRadius)
    {
        this.detectionAngle = detectionAngle;
        this.detectionRadius = detectionRadius;
        this.innerDetectionRadius = innerDetectionRadius;
    }

    public bool Execute(Transform player, Transform detector, CooldownTimer detectionCooldownTimer)
    {
        if (player == null || detector == null || detectionCooldownTimer.IsRunning)
        {
            return false;
        }

        var offsetToPlayer = player.position - detector.position;
        var distanceToPlayer = offsetToPlayer.magnitude;

        if (distanceToPlayer > detectionRadius)
        {
            return false;
        }

        if (distanceToPlayer <= innerDetectionRadius)
        {
            detectionCooldownTimer.Start();
            return true;
        }

        var directionToPlayer = offsetToPlayer.normalized;
        var angleToPlayer = Vector3.Angle(detector.right, directionToPlayer);

        // Внешний радиус требует, чтобы игрок был внутри конуса обзора.
        if (angleToPlayer > detectionAngle / 2f)
        {
            return false;
        }

        detectionCooldownTimer.Start();
        return true;
    }
}
