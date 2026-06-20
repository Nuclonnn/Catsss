using System.Collections;
using Catsss.Network;
using UnityEngine;

namespace Catsss.Menu.Flow.Services
{
    /// <summary>Stop NGO и загрузка MainMenu после выхода из сессии.</summary>
    internal static class AppFlowReturnToMenuService
    {
        private const string LogContext = nameof(ApplicationFlowController);

        public static IEnumerator RunReturnToMainMenu(
            IAppFlowRoutineHost host,
            string mainMenuSceneName,
            bool stopNetwork)
        {
            ConnectionManager manager = Object.FindAnyObjectByType<ConnectionManager>();

            if (stopNetwork && manager != null)
            {
                manager.Stop();
            }

            MenuLoadingOverlay.Instance?.HideAndDestroy();
            host.SetSessionStartupHandled(false);

            bool menuLoaded = false;
            yield return AppFlowSceneLoader.LoadMainMenuScene(mainMenuSceneName, success => menuLoaded = success);

            if (!menuLoaded)
            {
                Debug.LogError($"[{LogContext}] Не удалось загрузить сцену '{mainMenuSceneName}'.");
                yield break;
            }

            host.EnterMainMenuState();
        }
    }
}
