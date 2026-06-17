using UnityEngine;

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

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            _speedHash = Animator.StringToHash(speedParameter);
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
        }

        private float EvaluateNormalizedSpeed()
        {
            Transform root = motionRoot != null ? motionRoot : transform.parent;

            if (root == null || Time.deltaTime <= 0f)
            {
                return 0f;
            }

            Vector3 worldPosition = root.position;

            if (!_hasLastPosition)
            {
                _lastWorldPosition = worldPosition;
                _hasLastPosition = true;
                return 0f;
            }

            Vector3 delta = worldPosition - _lastWorldPosition;
            delta.y = 0f;
            _lastWorldPosition = worldPosition;

            float horizontalSpeed = delta.magnitude / Time.deltaTime;
            return Mathf.Clamp01(horizontalSpeed / ResolveSpeedForFullWalk());
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
