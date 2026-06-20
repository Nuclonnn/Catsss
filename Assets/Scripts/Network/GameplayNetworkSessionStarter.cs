using System.Collections;
using Catsss.Menu;
using Catsss.Menu.Flow;
using UnityEngine;

namespace Catsss.Network
{
    /// <summary>
    /// Dev-fallback NGO на gameplay-сцене: прямой Play Sandbox (host) или CLI <c>-join</c> (client).
    /// Основной путь — <see cref="ApplicationFlowController"/> через MainMenu.
    /// </summary>
    public sealed class GameplayNetworkSessionStarter : MonoBehaviour
    {
        [SerializeField]
        private bool fallbackStartHostWhenNoMenuIntent = true;

        [SerializeField]
        private string localhostTargetForJoinCli = "127.0.0.1";

        private IEnumerator Start()
        {
            if (ShouldSkipBecauseMenuFlowHandlesSession())
            {
                yield break;
            }

            ConnectionManager manager = FindAnyObjectByType<ConnectionManager>();

            if (DevelopmentJoinArgs.WantsClientFromCommandLine())
            {
                if (manager == null)
                {
                    Debug.LogError("[GameplayNetworkSessionStarter] ConnectionManager не найден (−join).");
                    TryHideDevOverlayOnly();
                    yield break;
                }

                manager.ConfigureForClient(localhostTargetForJoinCli.Trim(), manager.GameplayPort);
                manager.StartClient();
                TryHideDevOverlayOnly();
                NotifyDevSessionActive();
                yield break;
            }

            if (!fallbackStartHostWhenNoMenuIntent)
            {
                Debug.LogWarning(
                    "[GameplayNetworkSessionStarter] Fallback-хост отключён — сеть не стартует без MainMenu flow.");
                TryHideDevOverlayOnly();
                yield break;
            }

            if (manager == null)
            {
                Debug.LogError("[GameplayNetworkSessionStarter] ConnectionManager не найден.");
                TryHideDevOverlayOnly();
                yield break;
            }

            manager.StartHost();
            TryHideDevOverlayOnly();
            NotifyDevSessionActive();
        }

        private static bool ShouldSkipBecauseMenuFlowHandlesSession()
        {
            if (!AppFlow.TryGet(out IAppFlowCommands flow))
            {
                return false;
            }

            return flow.IsSessionStartupHandled || flow.IsLoadingFlow || flow.IsReturningFlow;
        }

        /// <summary>Не трогаем overlay, созданный MainMenu flow во время Loading/Returning.</summary>
        private static void TryHideDevOverlayOnly()
        {
            if (AppFlow.TryGet(out IAppFlowCommands flow)
                && (flow.IsLoadingFlow || flow.IsReturningFlow))
            {
                return;
            }

            MenuLoadingOverlay.Instance?.HideAndDestroy();
        }

        private static void NotifyDevSessionActive()
        {
            IAppFlowCommands flow = AppFlow.EnsureExists();
            flow.NotifyGameplaySessionActive();
        }
    }
}
