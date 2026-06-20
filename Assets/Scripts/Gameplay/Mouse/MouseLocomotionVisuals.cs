using UnityEngine;
using UnityEngine.AI;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>
    /// Анимация ходьбы мыши: Speed в Animator из фактической скорости MouseRoot (реплицируется с сервера).
    /// Логика движения остаётся в MouseBrain — здесь только визуал.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseLocomotionVisuals : MonoBehaviour
    {
        [Header("Logic")]
        [SerializeField] private MouseBrain mouseBrain;
        [SerializeField] private Transform motionRoot;

        [Header("Visual")]
        [SerializeField] private Animator animator;

        [Header("Animator")]
        [SerializeField] private string speedParameter = "Speed";
        [Tooltip("Горизонтальная скорость (м/с), при которой Speed в Animator = 1.")]
        [SerializeField, Min(0.1f)] private float speedForFullWalk = 4f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

        private int _speedHash;
        private Vector3 _lastWorldPosition;
        private bool _hasLastPosition;
        private NavMeshAgent _navMeshAgent;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            _speedHash = Animator.StringToHash(speedParameter);

#if UNITY_EDITOR
            if (animator != null && animator.avatar == null)
            {
                Debug.LogWarning(
                    "[MouseLocomotionVisuals] Animator без Avatar — кости не будут двигаться. " +
                    "Проверь mouse model.fbx: Rig → Generic, Create From This Model.",
                    this);
            }
#endif
        }

        private void OnEnable()
        {
            if (mouseBrain != null)
            {
                mouseBrain.PresenceModeChanged += OnPresenceModeChanged;
                ApplyPresence(mouseBrain.PresenceMode);
            }

            _hasLastPosition = false;
        }

        private void OnDisable()
        {
            if (mouseBrain != null)
            {
                mouseBrain.PresenceModeChanged -= OnPresenceModeChanged;
            }
        }

        private void Update()
        {
            if (animator == null || !animator.enabled)
            {
                return;
            }

            float normalizedSpeed = EvaluateNormalizedSpeed();
            animator.SetFloat(_speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
        }

        private void OnPresenceModeChanged(MousePresenceMode previous, MousePresenceMode current)
        {
            ApplyPresence(current);
        }

        private void ApplyPresence(MousePresenceMode mode)
        {
            if (animator == null)
            {
                return;
            }

            bool shouldAnimate = mode != MousePresenceMode.Hidden;
            animator.enabled = shouldAnimate;

            if (!shouldAnimate)
            {
                animator.SetFloat(_speedHash, 0f);
                _hasLastPosition = false;
            }
        }

        private void ResolveReferences()
        {
            if (mouseBrain == null)
            {
                mouseBrain = GetComponentInParent<MouseBrain>();
            }

            if (motionRoot == null && mouseBrain != null)
            {
                motionRoot = mouseBrain.transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (_navMeshAgent == null && mouseBrain != null)
            {
                mouseBrain.TryGetComponent(out _navMeshAgent);
            }
        }

        private float EvaluateNormalizedSpeed()
        {
            float horizontalSpeed = ResolveHorizontalSpeedMetersPerSecond();

            if (horizontalSpeed <= 0.01f)
            {
                return 0f;
            }

            return Mathf.Clamp01(horizontalSpeed / ResolveSpeedForFullWalk());
        }

        private float ResolveHorizontalSpeedMetersPerSecond()
        {
            float speedFromAgent = 0f;

            if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                Vector3 agentVelocity = _navMeshAgent.velocity;
                agentVelocity.y = 0f;
                speedFromAgent = agentVelocity.magnitude;
            }

            Transform root = motionRoot != null ? motionRoot : transform.parent;

            if (root == null || Time.deltaTime <= 0f)
            {
                return speedFromAgent;
            }

            Vector3 worldPosition = root.position;

            if (!_hasLastPosition)
            {
                _lastWorldPosition = worldPosition;
                _hasLastPosition = true;
                return speedFromAgent;
            }

            Vector3 delta = worldPosition - _lastWorldPosition;
            delta.y = 0f;
            _lastWorldPosition = worldPosition;

            float speedFromTransform = delta.magnitude / Time.deltaTime;
            return Mathf.Max(speedFromTransform, speedFromAgent);
        }

        private float ResolveSpeedForFullWalk()
        {
            if (mouseBrain != null && mouseBrain.Config != null)
            {
                return Mathf.Max(
                    speedForFullWalk,
                    mouseBrain.Config.RouteSpeed,
                    mouseBrain.Config.DomeAgentSpeed);
            }

            return speedForFullWalk;
        }
    }
}
