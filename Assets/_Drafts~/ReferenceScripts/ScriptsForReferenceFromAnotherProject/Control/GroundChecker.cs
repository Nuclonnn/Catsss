using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GroundChecker : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("Точка у стоп персонажа, откуда проверяется земля.")]
    [SerializeField] Transform groundCheckPoint;
    [Tooltip("Радиус сферы проверки земли.")]
    [SerializeField, Range(0.01f, 0.5f)] float groundDistance = 0.12f;
    [Tooltip("Слои, которые считаются землей.")]
    [SerializeField] LayerMask groundLayers;

    public bool IsGrounded { get; private set; }

    void Update()
    {
        UpdateGroundedState();
    }

    void FixedUpdate()
    {
        UpdateGroundedState();
    }

    void UpdateGroundedState()
    {
        if (groundCheckPoint == null)
        {
            IsGrounded = false;
            return;
        }

        IsGrounded = Physics.CheckSphere(
            groundCheckPoint.position,
            groundDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundDistance);
    }
}