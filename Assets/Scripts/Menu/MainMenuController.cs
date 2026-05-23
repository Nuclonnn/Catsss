using System.Collections;
using Catsss.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Catsss.Menu
{
    /// <summary>
    /// Простое меню: хост / гость-панель / выход. Загружает геймплей-сцену после постановки <see cref="NetworkSessionIntent"/>.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Цель")]
        [Tooltip("Имя сцены в Build Settings — по умолчанию Sandbox.")]
        [SerializeField]
        private string gameplaySceneName = "Sandbox";

        [Header("Настройка хоста")]
        [SerializeField]
        private ushort hostPort = NetworkSessionIntent.DefaultPort;

        [Tooltip("Локализованные динамические строки меню (IP-хинт, ошибка connect).")]
        [SerializeField]
        private MainMenuLocalizedText localizedText;

        [Header("Загрузка")]
        [SerializeField]
        private float minimumSecondsLoadingScreenDuringSceneLoad = 0.35f;

        [SerializeField]
        private float overlayHoldSecondsAfterConnect = 0.5f;

        [SerializeField]
        private Color loadingBackdropColorWithoutSprite = Color.black;

        [SerializeField]
        private Sprite loadingSplashSpriteOptional;

        [Header("Guest panel")]
        [SerializeField]
        private GameObject guestPanelRoot;

        [SerializeField]
        private InputField guestIpInputField;

        [SerializeField]
        private InputField guestPortInputField;

        [SerializeField]
        private string guestDefaultPortTextField = NetworkSessionIntent.DefaultPort.ToString();

        [Header("Кнопки")]
        [SerializeField]
        private Button hostButton;

        [SerializeField]
        private Button guestButtonOpenPanel;

        [SerializeField]
        private Button guestBackButton;

        [SerializeField]
        private Button guestConnectButton;

        [SerializeField]
        private Button quitButton;

        private bool _loadRoutineRunning;

        private void OnEnable()
        {
            _loadRoutineRunning = false;
            localizedText?.RefreshIpv4Hint();
            hostButton?.onClick.AddListener(OnHostClicked);
            guestButtonOpenPanel?.onClick.AddListener(OnGuestOpenClicked);
            guestBackButton?.onClick.AddListener(OnGuestBackClicked);
            guestConnectButton?.onClick.AddListener(OnGuestConnectClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            hostButton?.onClick.RemoveListener(OnHostClicked);
            guestButtonOpenPanel?.onClick.RemoveListener(OnGuestOpenClicked);
            guestBackButton?.onClick.RemoveListener(OnGuestBackClicked);
            guestConnectButton?.onClick.RemoveListener(OnGuestConnectClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);
        }

        private void Start()
        {
            ApplyReturnedGuestConnectionState();
            EnsureGuestPortFieldDefault();
        }

        private void ApplyReturnedGuestConnectionState()
        {
            if (!MenuConnectionFeedback.TryConsumeGuestReturn(out bool openGuestPanel, out ClientConnectInputError returnError))
            {
                localizedText?.ClearGuestError();

                if (guestPanelRoot != null)
                {
                    guestPanelRoot.SetActive(false);
                }

                return;
            }

            if (guestPanelRoot != null)
            {
                guestPanelRoot.SetActive(openGuestPanel);
            }

            if (returnError == ClientConnectInputError.None)
            {
                localizedText?.ShowConnectionError();
            }
            else
            {
                localizedText?.ShowConnectInputError(returnError);
            }
        }

        private void EnsureGuestPortFieldDefault()
        {
            if (guestPortInputField != null &&
                string.IsNullOrWhiteSpace(guestPortInputField.text))
            {
                guestPortInputField.text = guestDefaultPortTextField;
            }
        }

        private void OnHostClicked()
        {
            if (_loadRoutineRunning)
            {
                return;
            }

            localizedText?.ClearGuestError();
            NetworkSessionIntent.QueueHostLaunch(hostPort, overlayHoldSecondsAfterConnect);
            BeginGameplayLoadThroughOverlay();
        }

        private void OnGuestOpenClicked()
        {
            localizedText?.ClearGuestError();

            if (guestPanelRoot != null)
            {
                guestPanelRoot.SetActive(true);
                EnsureGuestPortFieldDefault();
            }
        }

        private void OnGuestBackClicked()
        {
            localizedText?.ClearGuestError();

            if (guestPanelRoot != null)
            {
                guestPanelRoot.SetActive(false);
            }
        }

        private void OnGuestConnectClicked()
        {
            if (_loadRoutineRunning)
            {
                return;
            }

            string ip = guestIpInputField != null ? guestIpInputField.text.Trim() : string.Empty;
            string portText = guestPortInputField != null ? guestPortInputField.text.Trim() : guestDefaultPortTextField;

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
            NetworkSessionIntent.QueueClientLaunch(ip, resolvedPort, overlayHoldSecondsAfterConnect);
            BeginGameplayLoadThroughOverlay();
        }

        private void OnQuitClicked()
        {
            ApplicationLifecycle.QuitOrExitPlayMode();
        }

        private void BeginGameplayLoadThroughOverlay()
        {
            _loadRoutineRunning = true;
            ToggleInteractables(false);

            MenuLoadingOverlay overlay = MenuLoadingOverlay.Create(loadingSplashSpriteOptional, loadingBackdropColorWithoutSprite);
            overlay.Show();

            string scene = gameplaySceneName;
            float minWait = Mathf.Max(0f, minimumSecondsLoadingScreenDuringSceneLoad);

            overlay.RunCoroutine(LoadGameplayAsync(overlay, scene, minWait, () => _loadRoutineRunning = false));
        }

        private static IEnumerator LoadGameplayAsync(MenuLoadingOverlay overlay, string gameplaySceneName, float minWait, System.Action onFinished)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            operation.allowSceneActivation = false;

            float elapsed = 0f;
            bool readyToActivate =
                Mathf.Approximately(minWait, 0f) &&
                operation.progress >= 0.899f;

            while (!readyToActivate)
            {
                elapsed += Time.deltaTime;
                readyToActivate =
                    elapsed >= minWait &&
                    operation.progress >= 0.899f;

                yield return null;
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            onFinished?.Invoke();
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

            if (guestBackButton != null)
            {
                guestBackButton.interactable = enabledButtons;
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
