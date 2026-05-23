using System;
using Catsss.Configs;
using Catsss.Core.FSM;
using Catsss.Player.States;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Catsss.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference dashAction;
        [SerializeField] private InputActionReference aimAction;
        [SerializeField] private InputActionReference interactAction;

        [Header("Movement")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform groundProbe;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly StateMachine _stateMachine = new();
        private Rigidbody _rigidbody;
        private Vector2 _moveInput;
        private float _jumpBufferCounter;
        private float _coyoteCounter;
        private float _dashCooldownCounter;
        private float _dashTimer;
        private float _verticalVelocity;
        private int _jumpsUsed;
        private bool _jumpHeld;
        private bool _dashRequested;
        private bool _isGrounded;
        private bool _isDashing;
        private bool _isAiming;
        private bool _throwRequested;
        private ChargeType _activeCharge;

        public event Action Jumped;
        public event Action Dashed;
        public event Action Interacted;
        public event Action AimStarted;
        public event Action AimCanceled;
        public event Action ThrowRequested;

        public Vector3 MoveDirection { get; private set; }
        public bool CanDash { get; private set; }
        public int MaxJumps { get; private set; } = 1;
        public bool IsHeavy => _activeCharge != null && _activeCharge.IsHeavy;
        public bool IsAiming => _isAiming;
        public bool IsDashing => _isDashing;
        public ChargeType ActiveCharge => _activeCharge;

        private GameConfig.PlayerMovementSettings MovementSettings => gameConfig.PlayerMovement;
        private float SpeedMultiplier => _activeCharge != null ? _activeCharge.SpeedMultiplier : 1f;
        private float JumpMultiplier => _activeCharge != null ? _activeCharge.JumpMultiplier : 1f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.useGravity = false;

            BuildStateMachine();
        }

        public override void OnNetworkSpawn()
        {
            enabled = IsOwner;
            SetInputEnabled(IsOwner);
        }

        private void OnDisable()
        {
            SetInputEnabled(false);
        }

        private void Update()
        {
            if (!IsOwner || gameConfig == null)
            {
                return;
            }

            ReadInput();
            TickTimers(Time.deltaTime);
            UpdateGrounded();
            _stateMachine.Update();
            DispatchBufferedActions();
        }

        private void FixedUpdate()
        {
            if (!IsOwner || gameConfig == null)
            {
                return;
            }

            _stateMachine.FixedUpdate();
        }

        public void ApplyCharge(ChargeType chargeType)
        {
            _activeCharge = chargeType;
        }

        public void UnlockAbility(string abilityName)
        {
            switch (abilityName)
            {
                case PlayerAbility.Dash:
                    CanDash = true;
                    break;
                case PlayerAbility.DoubleJump:
                    MaxJumps = 2;
                    break;
            }
        }

        public void Teleport(Vector3 position)
        {
            _rigidbody.position = position;
            _rigidbody.linearVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _jumpsUsed = 0;
        }

        public void AddExternalForce(Vector3 force, ForceMode forceMode)
        {
            _rigidbody.AddForce(force, forceMode);
        }

        public void StartJump()
        {
            if (!CanStartJump())
            {
                return;
            }

            _jumpBufferCounter = 0f;
            _coyoteCounter = 0f;
            _jumpsUsed++;

            float safeDuration = Mathf.Max(0.05f, MovementSettings.jumpDuration);
            float jumpHeight = Mathf.Max(0.1f, MovementSettings.jumpHeight * JumpMultiplier);
            _verticalVelocity = (2f * jumpHeight) / safeDuration;

            Jumped?.Invoke();
        }

        public void StartDash()
        {
            if (!CanStartDash())
            {
                return;
            }

            _dashRequested = false;
            _isDashing = true;
            _dashTimer = MovementSettings.dashDuration;
            _dashCooldownCounter = MovementSettings.dashCooldown;
            Dashed?.Invoke();
        }

        public void StopDash()
        {
            _isDashing = false;
            _dashTimer = 0f;
        }

        public void ApplyHorizontalMovement()
        {
            Vector3 targetVelocity = MoveDirection * MovementSettings.baseSpeed * SpeedMultiplier;
            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);
            Vector3 nextHorizontal = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                MovementSettings.acceleration * Time.fixedDeltaTime);

            _rigidbody.linearVelocity = new Vector3(nextHorizontal.x, velocity.y, nextHorizontal.z);
            RotateTowardsMoveDirection();
        }

        public void ApplyVerticalMovement()
        {
            if (_isGrounded && _verticalVelocity <= 0f)
            {
                _verticalVelocity = -0.5f;
                _jumpsUsed = 0;
            }

            float safeDuration = Mathf.Max(0.05f, MovementSettings.jumpDuration);
            float jumpHeight = Mathf.Max(0.1f, MovementSettings.jumpHeight * JumpMultiplier);
            float gravity = (-2f * jumpHeight) / (safeDuration * safeDuration);

            if (!_jumpHeld && _verticalVelocity > 0f)
            {
                gravity *= MovementSettings.jumpCutGravityMultiplier;
            }
            else if (_verticalVelocity < 0f)
            {
                gravity *= MovementSettings.gravityMultiplier;
            }

            _verticalVelocity += gravity * Time.fixedDeltaTime;

            Vector3 velocity = _rigidbody.linearVelocity;
            _rigidbody.linearVelocity = new Vector3(velocity.x, _verticalVelocity, velocity.z);
        }

        public void ApplyDashMovement()
        {
            _dashTimer = Mathf.Max(0f, _dashTimer - Time.fixedDeltaTime);
            Vector3 dashDirection = MoveDirection.sqrMagnitude > 0.001f ? MoveDirection : transform.forward;
            Vector3 velocity = dashDirection * MovementSettings.baseSpeed * MovementSettings.dashSpeedMultiplier;
            _rigidbody.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
            RotateTowardsMoveDirection(dashDirection);
        }

        public bool CanStartJump()
        {
            return _jumpBufferCounter > 0f && (_isGrounded || _coyoteCounter > 0f || _jumpsUsed < MaxJumps);
        }

        public bool HasFinishedJump()
        {
            return _isGrounded && _verticalVelocity <= 0f;
        }

        public bool CanStartDash()
        {
            return CanDash && _dashRequested && _dashCooldownCounter <= 0f;
        }

        public bool ShouldKeepDashing()
        {
            return _isDashing && _dashTimer > 0f;
        }

        private void BuildStateMachine()
        {
            var locomotion = new LocomotionState(this);
            var jump = new JumpState(this);
            var dash = new DashState(this);

            _stateMachine.AddAnyTransition(jump, new FuncPredicate(CanStartJump));
            _stateMachine.AddAnyTransition(dash, new FuncPredicate(CanStartDash));
            _stateMachine.AddTransition(jump, locomotion, new FuncPredicate(HasFinishedJump));
            _stateMachine.AddTransition(dash, locomotion, new FuncPredicate(() => !ShouldKeepDashing()));
            _stateMachine.SetState(locomotion);
        }

        private void ReadInput()
        {
            _moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : ReadKeyboardMove();
            _jumpHeld = ReadButton(jumpAction, Keyboard.current?.spaceKey);

            if (WasPressedThisFrame(jumpAction, Keyboard.current?.spaceKey))
            {
                _jumpBufferCounter = MovementSettings.jumpBufferTime;
            }

            if (WasPressedThisFrame(dashAction, Keyboard.current?.leftShiftKey))
            {
                _dashRequested = true;
            }

            bool aimHeld = ReadButton(aimAction, Mouse.current?.rightButton);
            if (aimHeld && !_isAiming)
            {
                _isAiming = true;
                AimStarted?.Invoke();
            }
            else if (!aimHeld && _isAiming)
            {
                _isAiming = false;
                _throwRequested = true;
                AimCanceled?.Invoke();
            }

            if (WasPressedThisFrame(interactAction, Keyboard.current?.eKey))
            {
                Interacted?.Invoke();
            }

            MoveDirection = CalculateMoveDirection(_moveInput);
        }

        private void DispatchBufferedActions()
        {
            if (_throwRequested)
            {
                _throwRequested = false;
                ThrowRequested?.Invoke();
            }
        }

        private void TickTimers(float deltaTime)
        {
            _jumpBufferCounter = Mathf.Max(0f, _jumpBufferCounter - deltaTime);
            _dashCooldownCounter = Mathf.Max(0f, _dashCooldownCounter - deltaTime);

            if (_isGrounded)
            {
                _coyoteCounter = MovementSettings.coyoteTime;
            }
            else
            {
                _coyoteCounter = Mathf.Max(0f, _coyoteCounter - deltaTime);
            }
        }

        private void UpdateGrounded()
        {
            Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position + Vector3.down * 0.45f;
            _isGrounded = Physics.CheckSphere(probePosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        }

        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            Vector3 raw = new(input.x, 0f, input.y);
            raw = Vector3.ClampMagnitude(raw, 1f);

            if (cameraTransform == null)
            {
                return raw;
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * raw.z + right * raw.x, 1f);
        }

        private void RotateTowardsMoveDirection()
        {
            RotateTowardsMoveDirection(MoveDirection);
        }

        private void RotateTowardsMoveDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                MovementSettings.rotationSpeed * Time.fixedDeltaTime);
        }

        private void SetInputEnabled(bool isEnabled)
        {
            SetActionEnabled(moveAction, isEnabled);
            SetActionEnabled(jumpAction, isEnabled);
            SetActionEnabled(dashAction, isEnabled);
            SetActionEnabled(aimAction, isEnabled);
            SetActionEnabled(interactAction, isEnabled);
        }

        private static void SetActionEnabled(InputActionReference reference, bool isEnabled)
        {
            if (reference == null)
            {
                return;
            }

            if (isEnabled)
            {
                reference.action.Enable();
            }
            else
            {
                reference.action.Disable();
            }
        }

        private static bool ReadButton(InputActionReference reference, ButtonControl fallback)
        {
            return reference != null ? reference.action.IsPressed() : fallback != null && fallback.isPressed;
        }

        private static bool WasPressedThisFrame(InputActionReference reference, ButtonControl fallback)
        {
            return reference != null ? reference.action.WasPressedThisFrame() : fallback != null && fallback.wasPressedThisFrame;
        }

        private static Vector2 ReadKeyboardMove()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            input.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
            input.x -= Keyboard.current.aKey.isPressed ? 1f : 0f;
            input.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
            input.y -= Keyboard.current.sKey.isPressed ? 1f : 0f;
            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
