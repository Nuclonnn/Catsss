using UnityEngine;

public interface IDetectionStrategy
{
    bool Execute(Transform player, Transform detector, CooldownTimer detectionCooldownTimer);
}
