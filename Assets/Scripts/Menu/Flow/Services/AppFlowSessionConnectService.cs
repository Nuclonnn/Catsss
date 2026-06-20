using System.Collections;
using Catsss.Menu.Flow.ReturnFeedback;
using Catsss.Network;
using UnityEngine;

namespace Catsss.Menu.Flow.Services
{
    /// <summary>Загрузка gameplay-сцены, overlay и NGO host/client connect из MainMenu.</summary>
    internal static class AppFlowSessionConnectService
    {
        private const string LogContext = nameof(ApplicationFlowController);

        public static IEnumerator RunSessionLoad(
            IAppFlowRoutineHost host,
            NetworkSessionIntent.LaunchPayload launch,
            MenuLoadPresentation presentation)
        {
            string sceneName = launch.LevelSceneName;
            MenuLoadingOverlay overlay = MenuLoadingOverlay.Create(
                presentation.LoadingSplashSpriteOptional,
                presentation.LoadingBackdropColorWithoutSprite);
            overlay.Show();

            bool sceneLoaded = false;
            yield return AppFlowSceneLoader.LoadSingleSceneWithMinimumWait(
                sceneName,
                presentation.MinimumSecondsLoadingScreen,
                success => sceneLoaded = success);

            if (!sceneLoaded)
            {
                Debug.LogError(
                    $"[{LogContext}] Не удалось загрузить сцену '{sceneName}'. " +
                    "Проверь имя в LevelCatalog и Build Profiles (File → Build Profiles).");
                MenuReturnFeedbackContext sceneContext = new MenuReturnFeedbackContext(sceneName: sceneName);
                AppFlowReturnFeedbackRegistry.Apply(MenuReturnReason.SceneLoadFailed, in sceneContext);
                overlay.HideAndDestroy();
                host.EnterReturningState();
                yield break;
            }

            ConnectionManager manager = Object.FindAnyObjectByType<ConnectionManager>();

            if (manager == null)
            {
                Debug.LogError($"[{LogContext}] ConnectionManager не найден на геймплей-сцене.");
                AppFlowReturnFeedbackRegistry.Apply(MenuReturnReason.SessionEnded);
                overlay.HideAndDestroy();
                host.EnterReturningState();
                yield break;
            }

            if (launch.IsHost)
            {
                yield return ConnectHost(host, manager, launch, overlay);
            }
            else
            {
                yield return ConnectClient(host, manager, launch, presentation, overlay);
            }
        }

        private static IEnumerator ConnectHost(
            IAppFlowRoutineHost host,
            ConnectionManager manager,
            NetworkSessionIntent.LaunchPayload launch,
            MenuLoadingOverlay overlay)
        {
            yield return NetworkSessionConnectRoutines.StartHost(
                manager,
                launch.Port,
                launch.HoldLoadingOverlaySecondsAfterConnect);

            host.SetSessionStartupHandled(true);
            overlay.HideAndDestroy();
            host.EnterInGameState();
        }

        private static IEnumerator ConnectClient(
            IAppFlowRoutineHost host,
            ConnectionManager manager,
            NetworkSessionIntent.LaunchPayload launch,
            MenuLoadPresentation presentation,
            MenuLoadingOverlay overlay)
        {
            NetworkSessionConnectRoutines.ClientConnectResult connectResult = default;

            yield return NetworkSessionConnectRoutines.ConnectClient(
                manager,
                new NetworkSessionConnectRoutines.ClientConnectParameters(
                    launch.ClientRemoteHost,
                    launch.Port,
                    presentation.ClientConnectTimeoutSeconds,
                    launch.HoldLoadingOverlaySecondsAfterConnect,
                    LogContext),
                result => connectResult = result);

            if (connectResult.IsConnected)
            {
                host.SetSessionStartupHandled(true);
                overlay.HideAndDestroy();
                host.EnterInGameState();
                yield break;
            }

            MenuReturnFeedbackContext guestContext = new MenuReturnFeedbackContext(
                guestLastHost: launch.ClientRemoteHost,
                guestLastPort: launch.Port,
                guestInputError: connectResult.InputError);

            if (connectResult.Outcome == NetworkSessionConnectRoutines.ClientConnectOutcome.InvalidInput)
            {
                AppFlowReturnFeedbackRegistry.Apply(MenuReturnReason.GuestInputError, in guestContext);
            }
            else
            {
                AppFlowReturnFeedbackRegistry.Apply(MenuReturnReason.GuestConnectionFailed, in guestContext);
            }

            yield return ShutdownNetworkAndReturn(host, manager, overlay);
        }

        private static IEnumerator ShutdownNetworkAndReturn(
            IAppFlowRoutineHost host,
            ConnectionManager manager,
            MenuLoadingOverlay overlay)
        {
            if (manager != null)
            {
                manager.Stop();
            }

            overlay.HideAndDestroy();
            host.EnterReturningState();
            yield break;
        }
    }
}
