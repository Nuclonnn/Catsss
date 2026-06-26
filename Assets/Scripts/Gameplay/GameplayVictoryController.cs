using Catsss.Gameplay.Mouse;
using Catsss.Menu;
using Catsss.Menu.Flow;
using Catsss.Menu.Levels;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Catsss.Core.Localization;

namespace Catsss.Gameplay
{
    /// <summary>
    /// Контроллер экрана победы. Показывается обоим игрокам при поимке мыши.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class GameplayVictoryController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Player.PlayerInputReader inputReader;

        [SerializeField]
        private Player.PlayerCameraController cameraController;

        [SerializeField]
        private LevelCatalog levelCatalog;

        [Header("Overlay")]
        [SerializeField]
        private GameObject victoryOverlayRoot;

        [SerializeField]
        private LocalizedUiText levelNameText;

        [Header("Buttons")]
        [SerializeField]
        private Button menuButton;

        private MouseBrain _mouseBrain;
        private bool _isVictoryTriggered;

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
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                if (victoryOverlayRoot != null)
                {
                    victoryOverlayRoot.SetActive(false);
                }
                return;
            }

            if (victoryOverlayRoot != null)
            {
                victoryOverlayRoot.SetActive(false);
            }

            _isVictoryTriggered = false;

            _mouseBrain = FindAnyObjectByType<MouseBrain>();
            if (_mouseBrain != null)
            {
                _mouseBrain.Caught += OnMouseCaught;
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(ReturnToMenu);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_mouseBrain != null)
            {
                _mouseBrain.Caught -= OnMouseCaught;
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ReturnToMenu);
            }
        }

        private void OnMouseCaught()
        {
            if (_isVictoryTriggered)
            {
                return;
            }
            _isVictoryTriggered = true;
            ShowVictoryScreen();
        }

        private void ShowVictoryScreen()
        {
            inputReader?.DisableInput();
            cameraController?.SetOwnerCursorLocked(false);

            if (levelNameText != null && levelCatalog != null)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                string levelKey = string.Empty;

                for (int i = 0; i < levelCatalog.Count; i++)
                {
                    if (levelCatalog.TryGetLevel(i, out LevelDefinition level) && level.SceneName == currentScene)
                    {
                        levelKey = level.DisplayNameKey;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(levelKey))
                {
                    if (levelNameText.TextReference.LocalizedText == null)
                    {
                        // Fallback, if LocalizedString is null, we can't easily set it.
                        // But we can set the editor fallback.
                        // Actually, we can just instantiate it via reflection or we should ensure it's created.
                    }
                    else
                    {
                        levelNameText.TextReference.LocalizedText.TableReference = "UI_Strings";
                        levelNameText.TextReference.LocalizedText.TableEntryReference = levelKey;
                    }
                    levelNameText.enabled = false;
                    levelNameText.enabled = true;
                }
            }

            if (victoryOverlayRoot != null)
            {
                victoryOverlayRoot.SetActive(true);
            }
        }

        private void ReturnToMenu()
        {
            cameraController?.SetOwnerCursorLocked(false);

            IAppFlowCommands flow = AppFlow.EnsureExists();

            if (flow.IsInGameFlow || flow.IsLoadingFlow)
            {
                flow.RequestReturnToMainMenu(MenuReturnReason.UserLeftSession, stopNetwork: true);
                return;
            }

            SceneManager.LoadScene(ApplicationFlowController.DefaultMainMenuSceneName);
        }
    }
}
