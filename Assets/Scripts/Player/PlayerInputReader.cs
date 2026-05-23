using UnityEngine;
using UnityEngine.InputSystem;

namespace Catsss.Player
{
    public sealed class PlayerInputReader : MonoBehaviour, global::InputSystem_Actions.IPlayerActions
    {
        private InputSystem_Actions _actions;
        private bool _jumpPressedThisFrame;
        private bool _dashPressedThisFrame;
        private bool _interactPressedThisFrame;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpHeld { get; private set; }
        /// <summary>Состояние кнопки рывка/бега (Shift) — удержание даёт бег после завершённого дэша.</summary>
        public bool SprintHeld { get; private set; }
        public bool IsLookInputFromMouse { get; private set; }

        private void Awake()
        {
            _actions = new InputSystem_Actions();
            _actions.Player.SetCallbacks(this);
            _actions.Player.Disable();
            ClearBufferedInput();
        }

        private void OnDestroy()
        {
            if (_actions == null)
            {
                return;
            }

            _actions.Player.RemoveCallbacks(this);
            _actions.Dispose();
        }

        public void EnableInput()
        {
            _actions?.Player.Enable();
        }

        public void DisableInput()
        {
            _actions?.Player.Disable();
            ClearBufferedInput();
        }

        /// <summary>Сброс «эджей» между клонами игроков (главным образом когда Enable ещё не вызывался).</summary>
        private void ClearBufferedInput()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
            IsLookInputFromMouse = false;
            _jumpPressedThisFrame = false;
            _dashPressedThisFrame = false;
            _interactPressedThisFrame = false;
        }

        public bool ConsumeInteractPressedThisFrame()
        {
            bool wasPressed = _interactPressedThisFrame;
            _interactPressedThisFrame = false;
            return wasPressed;
        }

        public bool ConsumeJumpPressedThisFrame()
        {
            bool wasPressed = _jumpPressedThisFrame;
            _jumpPressedThisFrame = false;
            return wasPressed;
        }

        public bool ConsumeDashPressedThisFrame()
        {
            bool wasPressed = _dashPressedThisFrame;
            _dashPressedThisFrame = false;
            return wasPressed;
        }

        /// <summary>Только проверка без сброса — для UX-подсказок, чтобы не перехватывать ввод у геймплея.</summary>
        public bool PeekDashPressedThisFrame() => _dashPressedThisFrame;

        public bool PeekInteractPressedThisFrame() => _interactPressedThisFrame;

        public bool PeekJumpPressedThisFrame() => _jumpPressedThisFrame;

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
            IsLookInputFromMouse = context.control != null && context.control.device is Mouse;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _jumpPressedThisFrame = true;
                JumpHeld = true;
            }
            else if (context.canceled)
            {
                JumpHeld = false;
            }
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _dashPressedThisFrame = true;
                SprintHeld = true;
            }
            else if (context.canceled)
            {
                SprintHeld = false;
            }
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _interactPressedThisFrame = true;
            }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
        }

        public void OnNext(InputAction.CallbackContext context)
        {
        }
    }
}
