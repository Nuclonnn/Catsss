using System.Collections;
using Catsss.Menu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Catsss.Network
{
    /// <summary>
    /// После перехода из MainMenu выполняет NGO connect по данным из <see cref="NetworkSessionIntent"/>.
    /// </summary>
    public sealed class GameplayNetworkSessionStarter : MonoBehaviour
    {
        [SerializeField]
        private bool fallbackStartHostWhenNoMenuIntent = true;

        [SerializeField]
        private string localhostTargetForJoinCli = "127.0.0.1";

        [Header("Client connect from menu")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [SerializeField, Min(1f)]
        private float clientConnectTimeoutSeconds = 10f;

        private IEnumerator Start()
        {
            ConnectionManager manager = FindAnyObjectByType<ConnectionManager>();

            if (NetworkSessionIntent.TryConsumeLaunch(out NetworkSessionIntent.LaunchPayload pending))
            {
                if (manager == null)
                {
                    Debug.LogError("GameplayNetworkSessionStarter: ConnectionManager не найден.");
                    MenuLoadingOverlay.Instance?.HideAndDestroy();
                    yield break;
                }

                if (pending.IsHost)
                {
                    yield return StartHostFromMenu(manager, pending);
                }
                else
                {
                    yield return StartClientFromMenu(manager, pending);
                }

                yield break;
            }

            if (DevelopmentJoinArgs.WantsClientFromCommandLine())
            {
                if (manager == null)
                {
                    Debug.LogError("GameplayNetworkSessionStarter (−join): ConnectionManager не найден.");
                    MenuLoadingOverlay.Instance?.HideAndDestroy();
                    yield break;
                }

                manager.ConfigureForClient(localhostTargetForJoinCli.Trim(), manager.GameplayPort);
                manager.StartClient();
                MenuLoadingOverlay.Instance?.HideAndDestroy();
                yield break;
            }

            if (!fallbackStartHostWhenNoMenuIntent)
            {
                Debug.LogWarning(
                    "GameplayNetworkSessionStarter: нет intent, −join выключён и fallback-хост запрещён — сеть не стартует.");
                MenuLoadingOverlay.Instance?.HideAndDestroy();
                yield break;
            }

            manager?.StartHost();
            MenuLoadingOverlay.Instance?.HideAndDestroy();
        }

        private IEnumerator StartHostFromMenu(ConnectionManager manager, NetworkSessionIntent.LaunchPayload pending)
        {
            manager.ConfigureForHost(pending.Port);
            manager.StartHost();

            if (pending.HoldLoadingOverlaySecondsAfterConnect > 0f)
            {
                yield return new WaitForSecondsRealtime(pending.HoldLoadingOverlaySecondsAfterConnect);
            }

            MenuLoadingOverlay.Instance?.HideAndDestroy();
        }

        private IEnumerator StartClientFromMenu(ConnectionManager manager, NetworkSessionIntent.LaunchPayload pending)
        {
            if (!ClientConnectInputValidator.TryValidate(pending.ClientRemoteHost, pending.Port, out ClientConnectInputError inputError))
            {
                Debug.LogWarning($"[GameplayNetworkSessionStarter] Rejected stored client input: {inputError}");
                MenuConnectionFeedback.SetPendingGuestInputError(inputError);
                yield return ReturnToGuestMenu(manager, stopNetwork: false);
                yield break;
            }

            bool connectionFailed;
            string failureLogMessage = null;

            void OnConnectionFailed(string message)
            {
                connectionFailed = true;
                failureLogMessage = message;
            }

            connectionFailed = false;
            manager.ConnectionFailed += OnConnectionFailed;

            manager.ConfigureForClient(pending.ClientRemoteHost, pending.Port);
            manager.StartClient();

            yield return null;

            if (connectionFailed)
            {
                manager.ConnectionFailed -= OnConnectionFailed;
                Debug.LogWarning($"[GameplayNetworkSessionStarter] Client connect failed immediately: {failureLogMessage}");
                MenuConnectionFeedback.SetPendingGuestConnectionError();
                yield return ReturnToGuestMenu(manager, stopNetwork: true);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < clientConnectTimeoutSeconds)
            {
                if (connectionFailed)
                {
                    break;
                }

                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager != null && networkManager.IsConnectedClient)
                {
                    manager.ConnectionFailed -= OnConnectionFailed;

                    if (pending.HoldLoadingOverlaySecondsAfterConnect > 0f)
                    {
                        yield return new WaitForSecondsRealtime(pending.HoldLoadingOverlaySecondsAfterConnect);
                    }

                    MenuLoadingOverlay.Instance?.HideAndDestroy();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            manager.ConnectionFailed -= OnConnectionFailed;
            Debug.LogWarning(
                connectionFailed
                    ? $"[GameplayNetworkSessionStarter] Client connect failed: {failureLogMessage}"
                    : $"[GameplayNetworkSessionStarter] Client connect timeout ({clientConnectTimeoutSeconds:0.#}s) to {pending.ClientRemoteHost}:{pending.Port}.");

            MenuConnectionFeedback.SetPendingGuestConnectionError();
            yield return ReturnToGuestMenu(manager, stopNetwork: true);
        }

        private IEnumerator ReturnToGuestMenu(ConnectionManager manager, bool stopNetwork)
        {
            if (stopNetwork && manager != null)
            {
                manager.Stop();
            }

            MenuLoadingOverlay.Instance?.HideAndDestroy();

            AsyncOperation loadMenu = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);

            if (loadMenu == null)
            {
                Debug.LogError($"[GameplayNetworkSessionStarter] Не удалось загрузить сцену '{mainMenuSceneName}'.");
                yield break;
            }

            while (!loadMenu.isDone)
            {
                yield return null;
            }
        }
    }
}
