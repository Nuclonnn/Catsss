using UnityEngine;
using DG.Tweening;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(Rigidbody))]
public class PlatformMover : MonoBehaviour
{
    [System.Serializable]
    public struct PlatformWaypoint
    {
        [Tooltip("Смещение позиции в мировых осях относительно стартовой позы платформы.")]
        public Vector3 positionOffset;

        [Tooltip("Поворот платформы относительно стартовой позы в градусах.")]
        public Vector3 rotationEuler;
    }

    [Header("Path")]
    [SerializeField, Tooltip("Точки пути: каждая хранит смещение позиции и поворот относительно стартовой позы платформы.")]
    PlatformWaypoint[] waypoints =
    {
        new PlatformWaypoint { positionOffset = Vector3.zero, rotationEuler = Vector3.zero },
        new PlatformWaypoint { positionOffset = new Vector3(0f, 0f, 3f), rotationEuler = Vector3.zero }
    };
    [SerializeField, Min(0.01f)] float moveTimePerSegment = 1f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [SerializeField] LoopType loopType = LoopType.Yoyo;

    [Header("Runtime (Read Only)")]
    [SerializeField] Vector3 platformDelta;
    [SerializeField] Vector3 platformVelocity;
    [SerializeField] Vector3 platformAngularVelocity;

    Rigidbody rb;
    Vector3 pathOriginPosition;
    Quaternion pathOriginRotation;
    int currentWaypointIndex;
    int direction = 1;
    float segmentProgress;

    public Vector3 PlatformVelocity => platformVelocity;
    public Vector3 PlatformDelta => platformDelta;
    public int WaypointCount => waypoints != null ? waypoints.Length : 0;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Start() {
        InitializePath();
    }

    void FixedUpdate() {
        if (waypoints == null || waypoints.Length < 2) {
            platformDelta = Vector3.zero;
            platformVelocity = Vector3.zero;
            platformAngularVelocity = Vector3.zero;
            return;
        }

        Vector3 currentPosition = rb.position;
        Quaternion currentRotation = rb.rotation;
        EvaluateNextPose(Time.fixedDeltaTime, out Vector3 nextPosition, out Quaternion nextRotation);
        platformDelta = nextPosition - currentPosition;
        platformVelocity = platformDelta / Time.fixedDeltaTime;
        platformAngularVelocity = CalculateAngularVelocity(currentRotation, nextRotation, Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);
        rb.MoveRotation(nextRotation);
    }

    void OnValidate() {
        moveTimePerSegment = Mathf.Max(0.01f, moveTimePerSegment);
    }

    void OnDrawGizmosSelected() {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        for (int i = 0; i < waypoints.Length; i++) {
            Vector3 point = GetWorldWaypointPosition(i);
            Gizmos.DrawSphere(point, 0.12f);
            DrawWaypointDirection(point, GetWorldWaypointRotation(i));

            if (i < waypoints.Length - 1) {
                Gizmos.DrawLine(point, GetWorldWaypointPosition(i + 1));
            }
        }

        if (loopType == LoopType.Restart && waypoints.Length > 2) {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            Gizmos.DrawLine(GetWorldWaypointPosition(waypoints.Length - 1), GetWorldWaypointPosition(0));
        }
    }

    void InitializePath() {
        if (waypoints == null || waypoints.Length == 0) {
            Debug.LogWarning("PlatformMover: добавь минимум 1 точку пути.", this);
            return;
        }

        pathOriginPosition = rb.position - waypoints[0].positionOffset;
        pathOriginRotation = rb.rotation * Quaternion.Inverse(Quaternion.Euler(waypoints[0].rotationEuler));
        currentWaypointIndex = 0;
        direction = 1;
        segmentProgress = 0f;
        platformDelta = Vector3.zero;
        platformVelocity = Vector3.zero;
        platformAngularVelocity = Vector3.zero;
        rb.position = GetWorldWaypointPosition(0, true);
        rb.rotation = GetWorldWaypointRotation(0, true);
    }

    void EvaluateNextPose(float deltaTime, out Vector3 nextPosition, out Quaternion nextRotation) {
        int nextWaypointIndex = GetNextWaypointIndex();
        if (nextWaypointIndex == currentWaypointIndex) {
            nextPosition = rb.position;
            nextRotation = rb.rotation;
            return;
        }

        segmentProgress += deltaTime / moveTimePerSegment;

        while (segmentProgress >= 1f) {
            segmentProgress -= 1f;
            currentWaypointIndex = nextWaypointIndex;
            nextWaypointIndex = GetNextWaypointIndex();

            if (nextWaypointIndex == currentWaypointIndex) {
                segmentProgress = 0f;
                nextPosition = GetWorldWaypointPosition(currentWaypointIndex, true);
                nextRotation = GetWorldWaypointRotation(currentWaypointIndex, true);
                return;
            }
        }

        float easedProgress = DOVirtual.EasedValue(0f, 1f, segmentProgress, ease);
        Vector3 fromPosition = GetWorldWaypointPosition(currentWaypointIndex, true);
        Vector3 toPosition = GetWorldWaypointPosition(nextWaypointIndex, true);
        Quaternion fromRotation = GetWorldWaypointRotation(currentWaypointIndex, true);
        Quaternion toRotation = GetWorldWaypointRotation(nextWaypointIndex, true);

        nextPosition = Vector3.LerpUnclamped(fromPosition, toPosition, easedProgress);
        nextRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, easedProgress);
    }

    int GetNextWaypointIndex() {
        int lastIndex = waypoints.Length - 1;
        if (lastIndex <= 0) {
            return currentWaypointIndex;
        }

        if (loopType == LoopType.Restart) {
            return (currentWaypointIndex + 1) % waypoints.Length;
        }

        int candidate = currentWaypointIndex + direction;
        if (candidate < 0 || candidate > lastIndex) {
            direction *= -1;
            candidate = currentWaypointIndex + direction;
        }

        return Mathf.Clamp(candidate, 0, lastIndex);
    }

    public Vector3 GetWorldWaypointPosition(int index) {
        return GetWorldWaypointPosition(index, false);
    }

    public Quaternion GetWorldWaypointRotation(int index) {
        return GetWorldWaypointRotation(index, false);
    }

    public Vector3 GetEditorOriginPosition() {
        if (waypoints == null || waypoints.Length == 0) {
            return transform.position;
        }

        return transform.position - waypoints[0].positionOffset;
    }

    public Quaternion GetEditorOriginRotation() {
        if (waypoints == null || waypoints.Length == 0) {
            return transform.rotation;
        }

        return transform.rotation * Quaternion.Inverse(Quaternion.Euler(waypoints[0].rotationEuler));
    }

    Vector3 GetWorldWaypointPosition(int index, bool useRuntimeOrigin) {
        if (waypoints == null || index < 0 || index >= waypoints.Length) {
            return transform.position;
        }

        Vector3 originPosition = useRuntimeOrigin || Application.isPlaying
            ? pathOriginPosition
            : GetEditorOriginPosition();
        return originPosition + waypoints[index].positionOffset;
    }

    Quaternion GetWorldWaypointRotation(int index, bool useRuntimeOrigin) {
        if (waypoints == null || index < 0 || index >= waypoints.Length) {
            return transform.rotation;
        }

        Quaternion originRotation = useRuntimeOrigin || Application.isPlaying
            ? pathOriginRotation
            : GetEditorOriginRotation();
        return originRotation * Quaternion.Euler(waypoints[index].rotationEuler);
    }

    Vector3 CalculateAngularVelocity(Quaternion from, Quaternion to, float deltaTime) {
        Quaternion deltaRotation = to * Quaternion.Inverse(from);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis);

        if (float.IsNaN(axis.x) || axis == Vector3.zero || Mathf.Approximately(deltaTime, 0f)) {
            return Vector3.zero;
        }

        if (angleInDegrees > 180f) {
            angleInDegrees -= 360f;
        }

        return axis.normalized * (angleInDegrees / deltaTime);
    }

    void DrawWaypointDirection(Vector3 position, Quaternion rotation) {
        Vector3 forward = rotation * Vector3.forward;
        Gizmos.DrawLine(position, position + forward * 0.5f);
    }
}
