using Catsss.Menu.Flow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Catsss.Menu
{
    /// <summary>
    /// Esc — «Назад» на подпанелях; A/D — карусель уровней (как стрелки).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MainMenuController))]
    public sealed class MainMenuKeyboardNavigation : MonoBehaviour
    {
        private MainMenuController _menuController;
        private InputAction _backAction;
        private InputAction _previousLevelAction;
        private InputAction _nextLevelAction;

        private void Awake()
        {
            _menuController = GetComponent<MainMenuController>();

            _backAction = CreateButtonAction("MenuBack");
            _backAction.AddBinding("<Keyboard>/escape");

            _previousLevelAction = CreateButtonAction("MenuLevelPrevious");
            _previousLevelAction.AddBinding("<Keyboard>/a");

            _nextLevelAction = CreateButtonAction("MenuLevelNext");
            _nextLevelAction.AddBinding("<Keyboard>/d");
        }

        private void OnEnable()
        {
            _backAction.performed += OnBackPerformed;
            _previousLevelAction.performed += OnPreviousLevelPerformed;
            _nextLevelAction.performed += OnNextLevelPerformed;
            _backAction.Enable();
            _previousLevelAction.Enable();
            _nextLevelAction.Enable();
        }

        private void OnDisable()
        {
            _backAction.performed -= OnBackPerformed;
            _previousLevelAction.performed -= OnPreviousLevelPerformed;
            _nextLevelAction.performed -= OnNextLevelPerformed;
            _backAction.Disable();
            _previousLevelAction.Disable();
            _nextLevelAction.Disable();
        }

        private void OnDestroy()
        {
            if (_backAction != null)
            {
                _backAction.Dispose();
                _backAction = null;
            }

            if (_previousLevelAction != null)
            {
                _previousLevelAction.Dispose();
                _previousLevelAction = null;
            }

            if (_nextLevelAction != null)
            {
                _nextLevelAction.Dispose();
                _nextLevelAction = null;
            }
        }

        private static InputAction CreateButtonAction(string name)
        {
            return new InputAction(
                name: name,
                type: InputActionType.Button,
                expectedControlType: "Button");
        }

        private void OnBackPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || !IsMenuKeyboardAllowed())
            {
                return;
            }

            _menuController.TryHandleSubPanelBack();
        }

        private void OnPreviousLevelPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || !IsMenuKeyboardAllowed())
            {
                return;
            }

            _menuController.TryNavigateLevelPrevious();
        }

        private void OnNextLevelPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || !IsMenuKeyboardAllowed())
            {
                return;
            }

            _menuController.TryNavigateLevelNext();
        }

        private static bool IsMenuKeyboardAllowed()
        {
            if (!AppFlow.TryGet(out IAppFlowCommands flow))
            {
                return true;
            }

            return flow.IsInMainMenuFlow && !flow.IsLoadingFlow && !flow.IsReturningFlow;
        }
    }
}
