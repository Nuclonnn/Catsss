using Catsss.Configs;
using Catsss.Player.Aim;
using Unity.Netcode;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>Орбитальная камера. После Cinemachine кэширует направление прицела для броска.</summary>
    public sealed class PlayerCameraController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkPlayerController playerController;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Camera unityCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private Transform target;

        public Camera UnityCamera => unityCamera;

        public CinemachineBrain CinemachineBrain => cinemachineBrain;

        /// <summary>Направление прицела после последнего тика Cinemachine (для броска в Update).</summary>
        public Vector3 CachedAimDirection { get; private set; } = Vector3.forward;

        public bool HasCachedAimDirection { get; private set; }

        [Header("Cinemachine Defaults")]
        [SerializeField] private bool applyOrbitalDefaultsOnSpawn = true;
        [SerializeField] private Vector3 targetOffset = new(0f, 1.35f, 0f);
        [SerializeField, Min(0.1f)] private float orbitRadius = 5f;
        [SerializeField] private Vector3 positionDamping = new(0.15f, 0.25f, 0.15f);
        [SerializeField] private Vector2 verticalRange = new(-80f, 80f);
        [SerializeField] private float initialVerticalAngle = 20f;
        [SerializeField] private float initialHorizontalAngle;

        [Header("Input")]
        [SerializeField, Min(0f)] private float mouseHorizontalSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float mouseVerticalSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadHorizontalSensitivity = 120f;
        [SerializeField, Min(0f)] private float gamepadVerticalSensitivity = 120f;
        [SerializeField] private bool invertVerticalInput;

        [Header("Owner Settings")]
        [SerializeField] private bool lockCursorForOwner = true;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponentInParent<NetworkPlayerController>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponentInParent<PlayerInputReader>();
            }

            if (unityCamera == null)
            {
                unityCamera = GetComponentInChildren<Camera>(true);
            }

            if (audioListener == null && unityCamera != null)
            {
                audioListener = unityCamera.GetComponent<AudioListener>();
            }

            if (cinemachineBrain == null && unityCamera != null)
            {
                cinemachineBrain = unityCamera.GetComponent<CinemachineBrain>();
            }

            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
            }

            if (orbitalFollow == null && cinemachineCamera != null)
            {
                orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
            }

            if (target == null)
            {
                target = playerController != null ? playerController.transform : transform;
            }
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCinemachineCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCinemachineCameraUpdated);
        }

        public override void OnNetworkSpawn()
        {
            SetCameraActive(IsOwner);

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            ConfigureCinemachineTargets();

            if (applyOrbitalDefaultsOnSpawn)
            {
                ApplyOrbitalDefaults();
            }

            if (playerController != null && unityCamera != null)
            {
                playerController.SetCameraTransform(unityCamera.transform);
            }


            if (lockCursorForOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            SetCameraActive(false);

            if (IsOwner && lockCursorForOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            if (!IsOwner || inputReader == null || orbitalFollow == null)
            {
                return;
            }

            UpdateCinemachineInput();
        }

        private void ConfigureCinemachineTargets()
        {
            if (cinemachineCamera == null || target == null)
            {
                return;
            }

            cinemachineCamera.Follow = target;
            cinemachineCamera.LookAt = target;
        }

        private void ApplyOrbitalDefaults()
        {
            if (orbitalFollow == null)
            {
                return;
            }

            orbitalFollow.TargetOffset = targetOffset;
            orbitalFollow.Radius = orbitRadius;
            orbitalFollow.TrackerSettings = new TrackerSettings
            {
                BindingMode = BindingMode.WorldSpace,
                PositionDamping = positionDamping,
                AngularDampingMode = AngularDampingMode.Euler,
                RotationDamping = Vector3.zero,
                QuaternionDamping = 0f
            };

            orbitalFollow.HorizontalAxis.Value = initialHorizontalAngle;
            orbitalFollow.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbitalFollow.HorizontalAxis.Wrap = true;
            orbitalFollow.VerticalAxis.Value = initialVerticalAngle;
            orbitalFollow.VerticalAxis.Range = verticalRange;
            orbitalFollow.VerticalAxis.Wrap = false;
        }

        private void UpdateCinemachineInput()
        {
            Vector2 lookInput = inputReader.LookInput;
            float verticalSign = invertVerticalInput ? 1f : -1f;

            if (inputReader.IsLookInputFromMouse)
            {
                orbitalFollow.HorizontalAxis.Value += lookInput.x * mouseHorizontalSensitivity;
                orbitalFollow.VerticalAxis.Value += lookInput.y * mouseVerticalSensitivity * verticalSign;
            }
            else
            {
                orbitalFollow.HorizontalAxis.Value += lookInput.x * gamepadHorizontalSensitivity * Time.deltaTime;
                orbitalFollow.VerticalAxis.Value += lookInput.y * gamepadVerticalSensitivity * verticalSign * Time.deltaTime;
            }

            orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                orbitalFollow.VerticalAxis.Value,
                orbitalFollow.VerticalAxis.Range.x,
                orbitalFollow.VerticalAxis.Range.y);
        }

        /// <summary>Вызывать после Cinemachine — кэш совпадает с кадром рендера.</summary>
        public void RefreshAimDirectionCache(Vector3 throwOriginWorld, ProjectileSettings projectileSettings, Transform ignoreRoot)
        {
            if (unityCamera == null)
            {
                HasCachedAimDirection = false;
                return;
            }

            CachedAimDirection = PlayerThrowDirectionResolver.Resolve(
                unityCamera,
                throwOriginWorld,
                projectileSettings,
                ignoreRoot);
            HasCachedAimDirection = true;
        }

        private void OnCinemachineCameraUpdated(CinemachineBrain brain)
        {
            if (!IsOwner || brain != cinemachineBrain || unityCamera == null)
            {
                return;
            }

            AimDirectionUpdated?.Invoke();
        }

        /// <summary>Срабатывает после обновления Output Camera (Cinemachine).</summary>
        public event System.Action AimDirectionUpdated;

        private void SetCameraActive(bool isActive)
        {
            if (unityCamera != null)
            {
                unityCamera.enabled = isActive;
            }

            if (audioListener != null)
            {
                audioListener.enabled = isActive;
            }

            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = isActive;
            }

            if (cinemachineCamera != null)
            {
                cinemachineCamera.enabled = isActive;
            }
        }
    }
}
