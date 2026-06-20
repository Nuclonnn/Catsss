using Catsss.Menu.Flow;
using Catsss.Menu.Levels;
using Catsss.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Catsss.Menu
{
    /// <summary>
    /// UI MainMenu: панели главная / гость / настройки. Загрузку и NGO делегирует <see cref="ApplicationFlowController"/>.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private MenuConfig menuConfig;

        [Tooltip("Локализованные динамические строки меню (IP-хинт, ошибки, баннеры).")]
        [SerializeField]
        private MainMenuLocalizedText localizedText;

        [Header("Panels")]
        [Tooltip("Не назначай на тот же объект, где висит MainMenuController — иначе скрипт отключится при смене панели.")]
        [SerializeField]
        private GameObject mainPanelRoot;

        [SerializeField]
        private GameObject guestPanelRoot;

        [SerializeField]
        private GameObject settingsPanelRoot;

        [SerializeField]
        private GameObject levelSelectPanelRoot;

        [SerializeField]
        private LevelSelectController levelSelectController;

        [Tooltip("Скрываются при открытии Guest/Settings, если mainPanelRoot не задан отдельно.")]
        [SerializeField]
        private GameObject mainMenuTitleRoot;

        [Header("Guest panel")]
        [SerializeField]
        private InputField guestIpInputField;

        [SerializeField]
        private InputField guestPortInputField;

        [Header("Кнопки")]
        [SerializeField]
        private Button hostButton;

        [SerializeField]
        private Button guestButtonOpenPanel;

        [SerializeField]
        private Button settingsButtonOpenPanel;

        [SerializeField]
        private Button guestBackButton;

        [SerializeField]
        private Button guestConnectButton;

        [SerializeField]
        private Button settingsBackButton;

        [SerializeField]
        private Button quitButton;

        private bool _sessionRequestPending;

        private void Awake()
        {
            if (menuConfig == null)
            {
                Debug.LogWarning(
                    "[MainMenuController] MenuConfig не назначен — используются runtime defaults. " +
                    "Catsss → Menu → Create Default Menu Config.",
                    this);
            }

            if (!TryGetComponent(out MainMenuKeyboardNavigation _))
            {
                gameObject.AddComponent<MainMenuKeyboardNavigation>();
            }
        }

        private void OnEnable()
        {
            _sessionRequestPending = false;
            localizedText?.RefreshIpv4Hint();
            hostButton?.onClick.AddListener(OnHostClicked);
            guestButtonOpenPanel?.onClick.AddListener(OnGuestOpenClicked);
            settingsButtonOpenPanel?.onClick.AddListener(OnSettingsOpenClicked);
            guestBackButton?.onClick.AddListener(OnGuestBackClicked);
            settingsBackButton?.onClick.AddListener(OnSettingsBackClicked);
            guestConnectButton?.onClick.AddListener(OnGuestConnectClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            hostButton?.onClick.RemoveListener(OnHostClicked);
            guestButtonOpenPanel?.onClick.RemoveListener(OnGuestOpenClicked);
            settingsButtonOpenPanel?.onClick.RemoveListener(OnSettingsOpenClicked);
            guestBackButton?.onClick.RemoveListener(OnGuestBackClicked);
            settingsBackButton?.onClick.RemoveListener(OnSettingsBackClicked);
            guestConnectButton?.onClick.RemoveListener(OnGuestConnectClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);
        }

        private void Start()
        {
            AppFlow.EnsureExists().NotifyMainMenuSceneLoaded();
            BindLevelSelect();
            ResetSessionRequestUiState();

            bool openedGuestPanel = ApplyReturnedMenuState();
            if (!openedGuestPanel)
            {
                EnsureGuestPortFieldDefault();
                ShowMainPanel();
            }
        }

        private void ResetSessionRequestUiState()
        {
            _sessionRequestPending = false;
            ToggleInteractables(true);
        }

        private void BindLevelSelect()
        {
            if (levelSelectController == null)
            {
                return;
            }

            levelSelectController.HostLevelRequested -= OnLevelHostRequested;
            levelSelectController.BackRequested -= OnLevelSelectBack;
            levelSelectController.HostLevelRequested += OnLevelHostRequested;
            levelSelectController.BackRequested += OnLevelSelectBack;
        }

        private void OnDestroy()
        {
            if (levelSelectController != null)
            {
                levelSelectController.HostLevelRequested -= OnLevelHostRequested;
                levelSelectController.BackRequested -= OnLevelSelectBack;
            }
        }

        private bool ApplyReturnedMenuState()
        {
            localizedText?.ClearGuestError();
            localizedText?.ClearSessionBanner();

            if (!MenuReturnFeedback.TryConsumeReturn(out MenuReturnFeedback.MenuReturnPayload payload))
            {
                return false;
            }

            if (payload.ShouldShowSessionBanner)
            {
                localizedText?.ShowSessionBanner(payload.Reason, payload.Context);
                ShowMainPanel();
                return false;
            }

            if (!payload.OpenGuestPanel)
            {
                ShowMainPanel();
                return false;
            }

            ApplyGuestFormSnapshot(payload);
            ShowGuestPanel();

            if (payload.GuestInputError == ClientConnectInputError.None)
            {
                localizedText?.ShowConnectionError();
            }
            else
            {
                localizedText?.ShowConnectInputError(payload.GuestInputError);
            }

            return true;
        }

        /// <summary>Восстанавливает IP/порт после возврата из неудачного guest connect.</summary>
        private void ApplyGuestFormSnapshot(MenuReturnFeedback.MenuReturnPayload payload)
        {
            if (!payload.HasGuestFormSnapshot)
            {
                EnsureGuestPortFieldDefault();
                return;
            }

            if (!string.IsNullOrWhiteSpace(payload.GuestLastHost) && guestIpInputField != null)
            {
                guestIpInputField.text = payload.GuestLastHost.Trim();
            }

            if (payload.GuestLastPort > 0 && guestPortInputField != null)
            {
                guestPortInputField.text = payload.GuestLastPort.ToString();
            }
            else
            {
                EnsureGuestPortFieldDefault();
            }
        }

        private void EnsureGuestPortFieldDefault()
        {
            if (guestPortInputField != null &&
                string.IsNullOrWhiteSpace(guestPortInputField.text))
            {
                guestPortInputField.text = ResolveGuestDefaultPortText();
            }
        }

        private void OnHostClicked()
        {
            if (_sessionRequestPending)
            {
                return;
            }

            localizedText?.ClearGuestError();
            localizedText?.ClearSessionBanner();
            ShowLevelSelectPanel();
        }

        private void OnLevelHostRequested(string levelSceneName)
        {
            if (_sessionRequestPending || string.IsNullOrWhiteSpace(levelSceneName))
            {
                return;
            }

            _sessionRequestPending = true;
            ToggleInteractables(false);

            IAppFlowCommands flow = AppFlow.EnsureExists();
            flow.RequestHostSession(ResolveHostPort(), levelSceneName, BuildLoadPresentation());
        }

        private void OnLevelSelectBack()
        {
            ShowMainPanel();
        }

        private void OnGuestOpenClicked()
        {
            localizedText?.ClearGuestError();
            localizedText?.ClearSessionBanner();
            ShowGuestPanel();
            EnsureGuestPortFieldDefault();
        }

        private void OnSettingsOpenClicked()
        {
            localizedText?.ClearGuestError();
            ShowSettingsPanel();
        }

        private void OnGuestBackClicked()
        {
            localizedText?.ClearGuestError();
            ShowMainPanel();
        }

        private void OnSettingsBackClicked()
        {
            ShowMainPanel();
        }

        private void OnGuestConnectClicked()
        {
            if (_sessionRequestPending)
            {
                return;
            }

            string ip = guestIpInputField != null ? guestIpInputField.text.Trim() : string.Empty;
            string portText = guestPortInputField != null
                ? guestPortInputField.text.Trim()
                : ResolveGuestDefaultPortText();

            if (string.IsNullOrWhiteSpace(ip))
            {
                localizedText?.ShowConnectInputError(ClientConnectInputError.EmptyHost);
                return;
            }

            if (!ClientConnectInputValidator.TryParsePort(portText, out ushort resolvedPort, out ClientConnectInputError portError))
            {
                localizedText?.ShowConnectInputError(portError);
                return;
            }

            if (!ClientConnectInputValidator.TryValidate(ip, resolvedPort, out ClientConnectInputError hostError))
            {
                localizedText?.ShowConnectInputError(hostError);
                return;
            }

            localizedText?.ClearGuestError();
            _sessionRequestPending = true;
            ToggleInteractables(false);

            IAppFlowCommands flow = AppFlow.EnsureExists();
            flow.RequestClientSession(ip, resolvedPort, ResolveGuestGameplaySceneName(), BuildLoadPresentation());
        }

        private void OnQuitClicked()
        {
            ApplicationLifecycle.QuitOrExitPlayMode();
        }

        /// <summary>Esc на подпанели — то же, что кнопка «Назад».</summary>
        public bool TryHandleSubPanelBack()
        {
            if (_sessionRequestPending)
            {
                return false;
            }

            if (IsPanelActive(guestPanelRoot))
            {
                OnGuestBackClicked();
                return true;
            }

            if (IsPanelActive(settingsPanelRoot))
            {
                OnSettingsBackClicked();
                return true;
            }

            if (IsPanelActive(levelSelectPanelRoot))
            {
                OnLevelSelectBack();
                return true;
            }

            return false;
        }

        /// <summary>A — предыдущий уровень в карусели.</summary>
        public bool TryNavigateLevelPrevious()
        {
            if (_sessionRequestPending || !IsPanelActive(levelSelectPanelRoot))
            {
                return false;
            }

            levelSelectController?.NavigatePrevious();
            return true;
        }

        /// <summary>D — следующий уровень в карусели.</summary>
        public bool TryNavigateLevelNext()
        {
            if (_sessionRequestPending || !IsPanelActive(levelSelectPanelRoot))
            {
                return false;
            }

            levelSelectController?.NavigateNext();
            return true;
        }

        private MenuLoadPresentation BuildLoadPresentation()
        {
            return menuConfig != null ? menuConfig.ToLoadPresentation() : MenuLoadPresentation.Default;
        }

        private ushort ResolveHostPort()
        {
            return menuConfig != null ? menuConfig.DefaultHostPort : NetworkSessionIntent.DefaultPort;
        }

        private string ResolveGuestGameplaySceneName()
        {
            if (menuConfig != null && !string.IsNullOrWhiteSpace(menuConfig.DefaultGameplaySceneName))
            {
                return menuConfig.DefaultGameplaySceneName;
            }

            return ApplicationFlowController.DefaultGameplaySceneName;
        }

        private string ResolveGuestDefaultPortText()
        {
            return ResolveHostPort().ToString();
        }

        private void ShowMainPanel()
        {
            SetOverlayPanelActive(guestPanelRoot, false);
            SetOverlayPanelActive(settingsPanelRoot, false);
            SetOverlayPanelActive(levelSelectPanelRoot, false);
            SetMainMenuContentVisible(true);
        }

        private void ShowGuestPanel()
        {
            SetMainMenuContentVisible(false);
            SetOverlayPanelActive(settingsPanelRoot, false);
            SetOverlayPanelActive(levelSelectPanelRoot, false);
            SetOverlayPanelActive(guestPanelRoot, true);
        }

        private void ShowSettingsPanel()
        {
            SetMainMenuContentVisible(false);
            SetOverlayPanelActive(guestPanelRoot, false);
            SetOverlayPanelActive(levelSelectPanelRoot, false);
            SetOverlayPanelActive(settingsPanelRoot, true);
        }

        private void ShowLevelSelectPanel()
        {
            SetMainMenuContentVisible(false);
            SetOverlayPanelActive(guestPanelRoot, false);
            SetOverlayPanelActive(settingsPanelRoot, false);
            SetOverlayPanelActive(levelSelectPanelRoot, true);
            levelSelectController?.ResetToFirstLevel();
        }

        private void SetOverlayPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel != gameObject)
            {
                panel.SetActive(active);
            }
        }

        private static bool IsPanelActive(GameObject panel)
        {
            return panel != null && panel.activeInHierarchy;
        }

        private void SetMainMenuContentVisible(bool visible)
        {
            if (CanToggleMainPanelRoot())
            {
                mainPanelRoot.SetActive(visible);
                return;
            }

            hostButton?.gameObject.SetActive(visible);
            guestButtonOpenPanel?.gameObject.SetActive(visible);
            settingsButtonOpenPanel?.gameObject.SetActive(visible);
            quitButton?.gameObject.SetActive(visible);

            if (mainMenuTitleRoot != null)
            {
                mainMenuTitleRoot.SetActive(visible);
            }
        }

        private bool CanToggleMainPanelRoot()
        {
            return mainPanelRoot != null && mainPanelRoot != gameObject;
        }

        private void ToggleInteractables(bool enabledButtons)
        {
            if (hostButton != null)
            {
                hostButton.interactable = enabledButtons;
            }

            if (guestButtonOpenPanel != null)
            {
                guestButtonOpenPanel.interactable = enabledButtons;
            }

            if (settingsButtonOpenPanel != null)
            {
                settingsButtonOpenPanel.interactable = enabledButtons;
            }

            if (guestBackButton != null)
            {
                guestBackButton.interactable = enabledButtons;
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.interactable = enabledButtons;
            }

            if (guestConnectButton != null)
            {
                guestConnectButton.interactable = enabledButtons;
            }

            if (quitButton != null)
            {
                quitButton.interactable = enabledButtons;
            }

            if (guestIpInputField != null)
            {
                guestIpInputField.interactable = enabledButtons;
            }

            if (guestPortInputField != null)
            {
                guestPortInputField.interactable = enabledButtons;
            }
        }
    }
}
