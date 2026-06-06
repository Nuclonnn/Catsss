using System;
using Catsss.Configs;
using Catsss.Configs.Charge;
using Catsss.Core.FSM;
using Catsss.Core.Timing;
using Catsss.Player.States;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerChargeController))]
    [RequireComponent(typeof(PlayerPermanentModifiers))]
    [RequireComponent(typeof(PlayerTrialInteractor))]
    [RequireComponent(typeof(AeroZoneReceiver))]
    public sealed class NetworkPlayerController : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Input")]
        [SerializeField] private PlayerInputReader inputReader;

        [Header("Scene References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform groundProbe;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.01f)] private float groundProbeRadius = 0.25f;

        [Header("Spawn (резервируется на сервере)")]
        [Tooltip("Ставить игроков в ряд при подключении, чтобы два префаба не стартовали в одной точке.")]
        [SerializeField] private bool applySequentialSpawnOffsets = true;

        [Tooltip("Смещение по мировому X для каждого OwnerClientId (хост часто id 0).")]
        [SerializeField] private float spawnStride = 2.5f;

        [Tooltip("Клиентские id могут быть большими — ограничиваем слот, чтобы не улететь далеко от сцены.")]
        [SerializeField] private ulong maxResolvedSpawnSlots = 32;

        [Header("Debug")]
        [SerializeField] private bool drawGroundDebug = true;
        [SerializeField] private Color groundedColor = Color.green;
        [SerializeField] private Color airborneColor = Color.red;

        private readonly StateMachine _stateMachine = new();
        private readonly CooldownTimer _jumpBufferTimer = new(0f);
        private readonly CooldownTimer _dashCooldownTimer = new(0f);
        private readonly CooldownTimer _dashDurationTimer = new(0f);
        private readonly GracePeriodTimer _coyoteTimer = new();
        private Rigidbody _rigidbody;
        private MovingPlatformRider _platformRider;
        private PlayerChargeController _chargeController;
        private PlayerPermanentModifiers _permanentModifiers;
        private AeroZoneReceiver _aeroZoneReceiver;
        private Vector2 _moveInput;
        private float _verticalVelocity;
        private bool _jumpHeld;
        private bool _dashRequested;
        private bool _isGrounded;
        private bool _isDashing;
        /// <summary>Включается при выходе из дэша, если Shift ещё зажат; сбрасывается при отпускании Shift или новом дэше.</summary>
        private bool _postDashSprintBoost;
        /// <summary>Пока true — можно сделать рывок в воздухе; один расход до касания земли.</summary>
        private bool _canUseAirDash = true;
        /// <summary>Текущий рывок начат в воздухе — не подмешиваем старое вертикальное ускорение прыжка.</summary>
        private bool _currentDashStartedAirborne;
        /// <summary>Для SmoothDamp плана движения относительно камеры.</summary>
        private Vector3 _smoothMoveDirectionVelocity;

        public event Action Jumped;
        public event Action Dashed;

        public Vector3 MoveDirection { get; private set; }
        public bool IsGrounded => _isGrounded;
        public bool IsDashing => _isDashing;

        private PlayerMovementSettings Movement => gameConfig.PlayerMovement;

        /// <summary>Мгновенное перемещение владельца (штраф, чекпоинт). Сбрасывает импульс и буферы ввода.</summary>
        public void TeleportTo(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!IsOwner)
            {
                return;
            }

            _verticalVelocity = 0f;
            _isDashing = false;
            _postDashSprintBoost = false;
            _canUseAirDash = true;
            _currentDashStartedAirborne = false;
            _dashRequested = false;
            _jumpHeld = false;
            _moveInput = Vector2.zero;
            MoveDirection = Vector3.zero;
            _smoothMoveDirectionVelocity = Vector3.zero;
            _dashDurationTimer.Clear();

            transform.SetPositionAndRotation(worldPosition, worldRotation);

            if (_rigidbody != null)
            {
                _rigidbody.position = worldPosition;
                _rigidbody.rotation = worldRotation;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>Сервер: телепорт только владельца этой копии игрока (client-auth движение).</summary>
        [ClientRpc]
        public void TeleportFromServerClientRpc(Vector3 worldPosition, Quaternion worldRotation)
        {
            TeleportTo(worldPosition, worldRotation);
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            _rigidbody = GetComponent<Rigidbody>();
            _platformRider = GetComponent<MovingPlatformRider>();
            _chargeController = GetComponent<PlayerChargeController>();
            _permanentModifiers = GetComponent<PlayerPermanentModifiers>();
            _aeroZoneReceiver = GetComponent<AeroZoneReceiver>();
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.useGravity = false;

            BuildStateMachine();
        }

        public override void OnNetworkSpawn()
        {
            ApplyOwnerSpawnStrideIfNeeded();

            enabled = IsOwner;

            if (IsOwner)
            {
                SetInputEnabled(true);
            }
            else
            {
                SetInputEnabled(false);
                _moveInput = Vector2.zero;
                MoveDirection = Vector3.zero;
                _smoothMoveDirectionVelocity = Vector3.zero;
                _dashRequested = false;
                _jumpHeld = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            SetInputEnabled(false);
        }

        private void OnDisable()
        {
            if (IsOwner)
            {
                SetInputEnabled(false);
            }
        }

        private void Update()
        {
            if (!IsOwner || gameConfig == null)
            {
                return;
            }

            ReadInput();
            UpdateGrounded();
            TickTimers(Time.deltaTime);
            _stateMachine.Update();
        }

        /// <summary>
        /// Направление движения относительно камеры считаем после CinemachineBrain (обычно в Late),
        /// иначе между Update и физикой копится рассогласование с интерполированным телом.
        /// </summary>
        private void LateUpdate()
        {
            if (!IsOwner || gameConfig == null)
            {
                return;
            }

            RebuildCameraRelativeMoveDirection();
        }

        private void FixedUpdate()
        {
            if (!IsOwner || gameConfig == null)
            {
                return;
            }

            _stateMachine.FixedUpdate();
            ApplyWindInfluence();
        }

        public bool CanStartJump()
        {
            ChargeTypeDefinition charge = _chargeController != null ? _chargeController.ActiveDefinition : null;

            if (charge != null && charge.SuppressJump)
            {
                return false;
            }

            return _jumpBufferTimer.Remaining > 0f && (_isGrounded || _coyoteTimer.HasGrace);
        }

        public bool HasFinishedJump()
        {
            return _isGrounded && _verticalVelocity <= 0f;
        }

        public bool CanStartDash()
        {
            if (_permanentModifiers != null && !_permanentModifiers.CanDash)
            {
                return false;
            }

            if (!_dashRequested || _dashCooldownTimer.Remaining > 0f)
            {
                return false;
            }

            return _isGrounded || _canUseAirDash;
        }

        public bool ShouldKeepDashing()
        {
            return _isDashing && _dashDurationTimer.IsRunning;
        }

        public void SetCameraTransform(Transform newCameraTransform)
        {
            cameraTransform = newCameraTransform;
        }

        private float GetMoveSpeedMultiplier()
        {
            float multiplier = _permanentModifiers != null ? _permanentModifiers.MoveSpeedMultiplier : 1f;
            ChargeTypeDefinition charge = _chargeController != null ? _chargeController.ActiveDefinition : null;

            if (charge != null)
            {
                multiplier *= charge.MoveSpeedMultiplier;
            }

            return Mathf.Max(0.01f, multiplier);
        }

        private float GetJumpHeightMultiplier()
        {
            ChargeTypeDefinition charge = _chargeController != null ? _chargeController.ActiveDefinition : null;
            return charge != null ? charge.JumpHeightMultiplier : 1f;
        }

        public void StartJump()
        {
            if (!CanStartJump())
            {
                return;
            }

            _jumpBufferTimer.Clear();
            _coyoteTimer.Clear();

            float safeDuration = Mathf.Max(0.05f, Movement.jumpDuration);
            float safeHeight = Mathf.Max(0.1f, Movement.jumpHeight) * GetJumpHeightMultiplier();
            _verticalVelocity = (2f * safeHeight) / safeDuration;
            Jumped?.Invoke();
        }

        public void StartDash()
        {
            if (!CanStartDash())
            {
                return;
            }

            _postDashSprintBoost = false;
            _dashRequested = false;
            _isDashing = true;
            _dashDurationTimer.Restart(Movement.dashDuration);
            _dashCooldownTimer.Restart(Movement.dashCooldown);

            _currentDashStartedAirborne = !_isGrounded;

            if (_currentDashStartedAirborne)
            {
                // Один воздушный рывок до приземления; обнуляем прыжок, иначе после дэша снова тянет вверх.
                _canUseAirDash = false;
                _verticalVelocity = 0f;
                Vector3 v = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(v.x, 0f, v.z);
            }

            Dashed?.Invoke();
        }

        public void StopDash()
        {
            _isDashing = false;
            _dashDurationTimer.Clear();
            _currentDashStartedAirborne = false;

            if (inputReader != null && inputReader.SprintHeld)
            {
                _postDashSprintBoost = true;
            }
        }

        public void ApplyHorizontalMovement()
        {
            float moveSpeed = Movement.baseSpeed * GetMoveSpeedMultiplier();
            if (_postDashSprintBoost && inputReader != null && inputReader.SprintHeld)
            {
                moveSpeed *= Movement.postDashSprintSpeedMultiplier;
            }

            Vector3 targetVelocity = MoveDirection * moveSpeed + GetPlatformCarryVelocity();
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 currentHorizontalVelocity = new(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetVelocity,
                Movement.acceleration * Time.fixedDeltaTime);

            _rigidbody.linearVelocity = new Vector3(nextHorizontalVelocity.x, currentVelocity.y, nextHorizontalVelocity.z);
            RotateTowards(MoveDirection);
        }

        public void ApplyVerticalMovement()
        {
            if (_isGrounded && _verticalVelocity <= 0f)
            {
                _verticalVelocity = -0.5f;
            }

            float safeDuration = Mathf.Max(0.05f, Movement.jumpDuration);
            float safeHeight = Mathf.Max(0.1f, Movement.jumpHeight);
            float gravity = (-2f * safeHeight) / (safeDuration * safeDuration);

            ChargeTypeDefinition charge = _chargeController != null ? _chargeController.ActiveDefinition : null;

            if (charge != null && charge.IsHeavy)
            {
                gravity *= charge.HeavyGravityMultiplier;
            }

            if (!_jumpHeld && _verticalVelocity > 0f)
            {
                gravity *= Movement.jumpCutGravityMultiplier;
            }
            else if (_verticalVelocity < 0f)
            {
                gravity *= Movement.gravityMultiplier;
            }

            _verticalVelocity += gravity * Time.fixedDeltaTime;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, _verticalVelocity, velocity.z);
        }

        public void ApplyDashMovement()
        {
            _dashDurationTimer.Tick(Time.fixedDeltaTime);
            Vector3 dashDirection = MoveDirection.sqrMagnitude > 0.001f ? MoveDirection : transform.forward;
            Vector3 velocity = dashDirection * Movement.baseSpeed * Movement.dashSpeedMultiplier + GetPlatformCarryVelocity();

            // На земле сохраняем «прилипание» из вертикальной логики; в воздухе Vy фиксируем — только гравитация после выхода из дэша.
            float vertical = _currentDashStartedAirborne ? 0f : _verticalVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, vertical, velocity.z);

            RotateTowards(dashDirection);
        }

        /// <summary>
        /// Ветер из AeroZone: target velocity + опциональная вертикальная пружина (equilibrium).
        /// Вызывается после FSM, в том числе во время Dash.
        /// </summary>
        public void ApplyWindInfluence()
        {
            if (_aeroZoneReceiver == null)
            {
                return;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            AeroZoneWindInfluence wind = _aeroZoneReceiver.SampleWindInfluence(velocity);

            if (!wind.HasInfluence)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            Vector3 targetVelocity = wind.TargetVelocity;

            if (_isGrounded && _platformRider != null && _platformRider.IsRidingPlatform)
            {
                targetVelocity += _platformRider.PlatformHorizontalVelocity;
            }

            Vector3 nextVelocity = Vector3.MoveTowards(
                velocity,
                targetVelocity,
                wind.ApproachAcceleration * deltaTime);

            if (wind.HasEquilibrium)
            {
                _verticalVelocity += wind.EquilibriumVerticalAcceleration * deltaTime;
                nextVelocity.y = _verticalVelocity;
            }
            else
            {
                _verticalVelocity = nextVelocity.y;
            }

            _rigidbody.linearVelocity = nextVelocity;
        }

        private void BuildStateMachine()
        {
            var locomotionState = new NetworkPlayerLocomotionState(this);
            var jumpState = new NetworkPlayerJumpState(this);
            var dashState = new NetworkPlayerDashState(this);

            _stateMachine.AddAnyTransition(jumpState, new FuncPredicate(CanStartJump));
            _stateMachine.AddAnyTransition(dashState, new FuncPredicate(CanStartDash));
            _stateMachine.AddTransition(jumpState, locomotionState, new FuncPredicate(HasFinishedJump));
            _stateMachine.AddTransition(dashState, locomotionState, new FuncPredicate(() => !ShouldKeepDashing()));
            _stateMachine.SetState(locomotionState);
        }

        private void ReadInput()
        {
            if (inputReader == null)
            {
                _moveInput = Vector2.zero;
                _jumpHeld = false;
                MoveDirection = Vector3.zero;
                _smoothMoveDirectionVelocity = Vector3.zero;
                _postDashSprintBoost = false;
                _canUseAirDash = true;
                return;
            }

            _moveInput = inputReader.MoveInput;
            _jumpHeld = inputReader.JumpHeld;

            if (!inputReader.SprintHeld)
            {
                _postDashSprintBoost = false;
            }

            if (inputReader.ConsumeJumpPressedThisFrame())
            {
                _jumpBufferTimer.Restart(Movement.jumpBufferTime);
            }

            if (inputReader.ConsumeDashPressedThisFrame())
            {
                _dashRequested = true;
            }
        }

        /// <summary>
        /// Обновляет <see cref="MoveDirection"/> в плоскости XZ с опциональным сглаживанием (настройка в конфиге).
        /// </summary>
        private void RebuildCameraRelativeMoveDirection()
        {
            Vector3 desired = CalculateMoveDirection(_moveInput);

            float smoothTime = Movement.cameraRelativeDirectionSmoothTime;
            if (smoothTime <= 1e-5f)
            {
                MoveDirection = desired;
                _smoothMoveDirectionVelocity = Vector3.zero;
                return;
            }

            Vector3 smoothed = Vector3.SmoothDamp(
                MoveDirection,
                desired,
                ref _smoothMoveDirectionVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);

            smoothed.y = 0f;
            if (smoothed.sqrMagnitude > 1f)
            {
                smoothed = Vector3.ClampMagnitude(smoothed, 1f);
            }

            MoveDirection = smoothed;
        }

        private void TickTimers(float deltaTime)
        {
            _jumpBufferTimer.Tick(deltaTime);
            _dashCooldownTimer.Tick(deltaTime);
            _coyoteTimer.Tick(deltaTime, _isGrounded, Movement.coyoteTime);
        }

        private void UpdateGrounded()
        {
            Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position + Vector3.down * 0.45f;
            _isGrounded = Physics.CheckSphere(probePosition, groundProbeRadius, groundMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                _canUseAirDash = true;
            }

            if (drawGroundDebug)
            {
                Debug.DrawRay(probePosition, Vector3.down * groundProbeRadius, _isGrounded ? groundedColor : airborneColor);
            }
        }

        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            Vector3 rawDirection = new(input.x, 0f, input.y);
            rawDirection = Vector3.ClampMagnitude(rawDirection, 1f);

            if (cameraTransform == null)
            {
                return rawDirection;
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * rawDirection.z + right * rawDirection.x, 1f);
        }

        /// <summary>
        /// На платформе целевая скорость включает движение платформы — иначе контроллер «тормозит» относительно неё.
        /// </summary>
        private Vector3 GetPlatformCarryVelocity()
        {
            if (!_isGrounded || _platformRider == null || !_platformRider.IsRidingPlatform)
            {
                return Vector3.zero;
            }

            return _platformRider.PlatformHorizontalVelocity;
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion next = Quaternion.RotateTowards(
                _rigidbody.rotation,
                targetRotation,
                Movement.rotationSpeed * Time.fixedDeltaTime);

            _rigidbody.MoveRotation(next);
        }

        private void SetInputEnabled(bool isEnabled)
        {
            if (inputReader == null)
            {
                return;
            }

            if (isEnabled)
            {
                inputReader.EnableInput();
                return;
            }

            inputReader.DisableInput();
        }

        /// <summary>
        /// При клиент-авторитете позицию задаёт только владелец копии — прокси и слушатель сервера копируют значение через NetworkTransform.
        /// </summary>
        private void ApplyOwnerSpawnStrideIfNeeded()
        {
            if (!IsOwner)
            {
                return;
            }

            if (!applySequentialSpawnOffsets || spawnStride == 0f)
            {
                return;
            }

            ulong maxSlot = maxResolvedSpawnSlots > 0 ? maxResolvedSpawnSlots - 1ul : 0ul;
            ulong slot = OwnerClientId < maxSlot ? OwnerClientId : maxSlot;
            Vector3 bumped = transform.position + new Vector3(spawnStride * slot, 0f, 0f);
            Quaternion rot = transform.rotation;

            transform.SetPositionAndRotation(bumped, rot);
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_rigidbody != null)
            {
                _rigidbody.position = bumped;
                _rigidbody.rotation = rot;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGroundDebug)
            {
                return;
            }

            Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position + Vector3.down * 0.45f;
            Gizmos.color = Application.isPlaying && _isGrounded ? groundedColor : airborneColor;
            Gizmos.DrawWireSphere(probePosition, groundProbeRadius);
            Gizmos.DrawLine(probePosition, probePosition + Vector3.down * groundProbeRadius);
        }
    }
}
