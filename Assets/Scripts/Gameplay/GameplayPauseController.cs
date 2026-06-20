using Catsss.Menu;
using Catsss.Menu.Flow;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Catsss.Gameplay
{
    /// <summary>
    /// Локальная пауза owner-клиента: Esc, курсор, блок ввода. Мир и напарник продолжают идти.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class GameplayPauseController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Player.PlayerInputReader inputReader;

        [SerializeField]
        private Player.PlayerCameraController cameraController;

        [Header("Overlay")]
        [SerializeField]
        private GameObject pauseOverlayRoot;

        [SerializeField]
        private GameObject pauseMenuPanel;

        [SerializeField]
        private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField]
        private Button resumeButton;

        [SerializeField]
        private Button settingsButton;

        [SerializeField]
        private Button exitSessionButton;

        [SerializeField]
        private Button settingsBackButton;

        private InputAction _pauseAction;
        private bool _isPaused;
        private bool _settingsVisible;
        private bool _exitRequested;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<Player.PlayerInputReader>();
            }

            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<Player.PlayerCameraController>(true);
            }

            _pauseAction = new InputAction(
                name: "Pause",
                type: InputActionType.Button,
                expectedControlType: "Button");
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
        }

        public override void OnDestroy()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPauseActionPerformed;
                _pauseAction.Disable();
                _pauseAction.Dispose();
                _pauseAction = null;
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;

                if (pauseOverlayRoot != null)
                {
                    pauseOverlayRoot.SetActive(false);
                }

                return;
            }

            _exitRequested = false;
            ForceResumeSilently();
            _pauseAction.performed += OnPauseActionPerformed;
            _pauseAction.Enable();
            BindButtons();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            UnbindButtons();
            _pauseAction.performed -= OnPauseActionPerformed;
            _pauseAction.Disable();
            ForceResumeSilently();
        }

        private void BindButtons()
        {
            resumeButton?.onClick.AddListener(Resume);
            settingsButton?.onClick.AddListener(ShowSettings);
            exitSessionButton?.onClick.AddListener(RequestExitSession);
            settingsBackButton?.onClick.AddListener(HideSettings);
        }

        private void UnbindButtons()
        {
            resumeButton?.onClick.RemoveListener(Resume);
            settingsButton?.onClick.RemoveListener(ShowSettings);
            exitSessionButton?.onClick.RemoveListener(RequestExitSession);
            settingsBackButton?.onClick.RemoveListener(HideSettings);
        }

        private void OnPauseActionPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || _exitRequested)
            {
                return;
            }

            if (_settingsVisible)
            {
                HideSettings();
                return;
            }

            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (_isPaused || _exitRequested)
            {
                return;
            }

            _isPaused = true;
            _settingsVisible = false;
            inputReader?.DisableInput();
            cameraController?.SetOwnerCursorLocked(false);

            if (pauseOverlayRoot != null)
            {
                pauseOverlayRoot.SetActive(true);
            }

            ShowPauseMenuPanel();
        }

        public void Resume()
        {
            if (!_isPaused || _exitRequested)
            {
                return;
            }

            _isPaused = false;
            _settingsVisible = false;
            inputReader?.EnableInput();
            cameraController?.SetOwnerCursorLocked(true);
            HideOverlayPanels();
        }

        private void ShowSettings()
        {
            if (!_isPaused)
            {
                return;
            }

            _settingsVisible = true;

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void HideSettings()
        {
            if (!_isPaused)
            {
                return;
            }

            _settingsVisible = false;
            ShowPauseMenuPanel();
        }

        private void ShowPauseMenuPanel()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void HideOverlayPanels()
        {
            if (pauseOverlayRoot != null)
            {
                pauseOverlayRoot.SetActive(false);
            }

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void RequestExitSession()
        {
            if (_exitRequested)
            {
                return;
            }

            _exitRequested = true;
            _isPaused = false;
            _settingsVisible = false;
            HideOverlayPanels();
            cameraController?.SetOwnerCursorLocked(false);

            IAppFlowCommands flow = AppFlow.EnsureExists();

            if (flow.IsInGameFlow || flow.IsLoadingFlow)
            {
                flow.RequestReturnToMainMenu(MenuReturnReason.UserLeftSession, stopNetwork: true);
                return;
            }

            Debug.LogWarning("[GameplayPauseController] Flow не в InGame — прямой возврат в MainMenu.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(ApplicationFlowController.DefaultMainMenuSceneName);
        }

        private void ForceResumeSilently()
        {
            _isPaused = false;
            _settingsVisible = false;
            HideOverlayPanels();
        }
    }
}
