using UnityEngine;

namespace Catsss.Player
{
    /// <summary>
    /// Визуал игрока: Animator, VFX заряда и tint по AuraColor.
    /// Подписывается на события логики/сети — без обратных ссылок из NetworkPlayerController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [Header("Logic (на PlayerRoot)")]
        [SerializeField] private NetworkPlayerController controller;
        [SerializeField] private NetworkPlayerEventsRelay eventsRelay;
        [SerializeField] private PlayerChargeController chargeController;
        [SerializeField] private Rigidbody body;

        [Header("Visual")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform chargeVfxRoot;
        [SerializeField] private Renderer[] tintRenderers;
        [SerializeField] private string colorPropertyName = "_BaseColor";

        [Header("Animator")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string dashTrigger = "Dash";
        [SerializeField] private string hasChargeBool = "HasCharge";
        [Tooltip("Горизонтальная скорость (м/с), при которой Speed в Animator = 1.")]
        [SerializeField, Min(0.1f)] private float speedForFullWalk = 5f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.1f;

        private MaterialPropertyBlock _propertyBlock;
        private int _colorPropertyId;
        private int _speedHash;
        private int _jumpHash;
        private int _dashHash;
        private int _hasChargeHash;
        private GameObject _currentChargeVfx;

        private void Reset()
        {
            controller = GetComponentInParent<NetworkPlayerController>();
            eventsRelay = GetComponentInParent<NetworkPlayerEventsRelay>();
            chargeController = GetComponentInParent<PlayerChargeController>();
            body = GetComponentInParent<Rigidbody>();
            animator = GetComponentInChildren<Animator>(true);

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Awake()
        {
            ResolveReferences();
            CacheAnimatorHashes();
            _propertyBlock = new MaterialPropertyBlock();
            _colorPropertyId = Shader.PropertyToID(colorPropertyName);
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Jumped += PlayJump;
                controller.Dashed += PlayDash;
            }

            if (eventsRelay != null)
            {
                eventsRelay.RemoteJumped += PlayJump;
                eventsRelay.RemoteDashed += PlayDash;
            }

            if (chargeController != null)
            {
                chargeController.ChargeChanged += OnChargeChanged;
            }

            ApplyChargeVisual(chargeController != null ? chargeController.ChargeId : (byte)0);
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Jumped -= PlayJump;
                controller.Dashed -= PlayDash;
            }

            if (eventsRelay != null)
            {
                eventsRelay.RemoteJumped -= PlayJump;
                eventsRelay.RemoteDashed -= PlayDash;
            }

            if (chargeController != null)
            {
                chargeController.ChargeChanged -= OnChargeChanged;
            }

            ClearChargeVfx();
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            float normalizedSpeed = EvaluateNormalizedSpeed();
            animator.SetFloat(_speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
        }

        private void ResolveReferences()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<NetworkPlayerController>();
            }

            if (eventsRelay == null)
            {
                eventsRelay = GetComponentInParent<NetworkPlayerEventsRelay>();
            }

            if (chargeController == null)
            {
                chargeController = GetComponentInParent<PlayerChargeController>();
            }

            if (body == null)
            {
                body = GetComponentInParent<Rigidbody>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (chargeVfxRoot == null)
            {
                chargeVfxRoot = transform;
            }

            if (tintRenderers == null || tintRenderers.Length == 0)
            {
                tintRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void CacheAnimatorHashes()
        {
            _speedHash = Animator.StringToHash(speedParameter);
            _jumpHash = Animator.StringToHash(jumpTrigger);
            _dashHash = Animator.StringToHash(dashTrigger);
            _hasChargeHash = Animator.StringToHash(hasChargeBool);
        }

        private float EvaluateNormalizedSpeed()
        {
            if (controller != null && controller.IsOwner)
            {
                return Mathf.Clamp01(controller.MoveDirection.magnitude);
            }

            if (body == null)
            {
                return 0f;
            }

            Vector3 horizontalVelocity = body.linearVelocity;
            horizontalVelocity.y = 0f;
            return Mathf.Clamp01(horizontalVelocity.magnitude / speedForFullWalk);
        }

        private void PlayJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(_jumpHash);
            }
        }

        private void PlayDash()
        {
            if (animator != null)
            {
                animator.SetTrigger(_dashHash);
            }
        }

        private void OnChargeChanged(byte chargeId, int trialId)
        {
            ApplyChargeVisual(chargeId);
        }

        private void ApplyChargeVisual(byte chargeId)
        {
            bool hasCharge = chargeId != 0;

            if (animator != null)
            {
                animator.SetBool(_hasChargeHash, hasCharge);
            }

            ClearChargeVfx();
            ApplyAuraTint(hasCharge ? chargeController?.ActiveDefinition?.AuraColor ?? Color.white : Color.white);

            if (!hasCharge || chargeController?.ActiveDefinition == null)
            {
                return;
            }

            GameObject prefab = chargeController.ActiveDefinition.VfxPrefab;
            if (prefab == null)
            {
                return;
            }

            _currentChargeVfx = Instantiate(prefab, chargeVfxRoot);
            _currentChargeVfx.transform.localPosition = Vector3.zero;
            _currentChargeVfx.transform.localRotation = Quaternion.identity;
        }

        private void ClearChargeVfx()
        {
            if (_currentChargeVfx != null)
            {
                Destroy(_currentChargeVfx);
                _currentChargeVfx = null;
            }
        }

        private void ApplyAuraTint(Color tint)
        {
            if (tintRenderers == null)
            {
                return;
            }

            foreach (Renderer renderer in tintRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_colorPropertyId, tint);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
