using UnityEngine;

namespace Catsss.Configs.Mouse
{
    /// <summary>Баланс и слои мыши. Назначается на MouseBrain.</summary>
    [CreateAssetMenu(menuName = "Catsss/Configs/Mouse Config")]
    public sealed class MouseConfig : ScriptableObject
    {
        [Header("Layers")]
        [Tooltip("Слой по умолчанию: мышь видна, но не сталкивается с игроками.")]
        [SerializeField] private LayerMask spectralLayer;

        [Tooltip("Только финальный купол: мышь может быть поймана.")]
        [SerializeField] private LayerMask physicalLayer;

        [Header("Route (Stage 6.2+)")]
        [SerializeField, Min(0.01f)] private float routeSpeed = 4f;
        [SerializeField, Min(1f)] private float routeTurnSpeedDegPerSec = 720f;

        [Header("Visual (client stub)")]
        [SerializeField, Min(0.01f)] private float presenceFadeDuration = 0.25f;

        [Header("Chase Rubberband (Stage 6.6+)")]
        [SerializeField, Min(0f)] private float rubberbandFarDistance = 15f;
        [SerializeField, Range(0.05f, 1f)] private float rubberbandSlowMultiplier = 0.3f;
        [SerializeField, Min(0f)] private float rubberbandNearDistance = 5f;

        [Header("Dome (Stage 6.7+)")]
        [Tooltip("Радиус поиска NavMesh при warp агента в куполе (м).")]
        [SerializeField, Min(0.5f)] private float domeNavMeshSampleRadius = 5f;
        [SerializeField, Min(0.01f)] private float domeFleeDistance = 6f;
        [SerializeField, Min(0.01f)] private float domeAgentSpeed = 3.5f;

        public LayerMask SpectralLayer => spectralLayer;
        public LayerMask PhysicalLayer => physicalLayer;
        public float RouteSpeed => routeSpeed;
        public float RouteTurnSpeedDegPerSec => routeTurnSpeedDegPerSec;
        public float PresenceFadeDuration => presenceFadeDuration;
        public float RubberbandFarDistance => rubberbandFarDistance;
        public float RubberbandSlowMultiplier => rubberbandSlowMultiplier;
        public float RubberbandNearDistance => rubberbandNearDistance;
        public float DomeNavMeshSampleRadius => domeNavMeshSampleRadius;
        public float DomeFleeDistance => domeFleeDistance;
        public float DomeAgentSpeed => domeAgentSpeed;
    }
}
