using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Catsss.Menu.Flow.Services
{
    /// <summary>Загрузка сцен с минимальным временем splash и возврат в MainMenu.</summary>
    internal static class AppFlowSceneLoader
    {
        public static string ResolveLevelSceneName(string levelSceneName)
        {
            return string.IsNullOrWhiteSpace(levelSceneName)
                ? ApplicationFlowController.DefaultGameplaySceneName
                : levelSceneName.Trim();
        }

        public static IEnumerator LoadSingleSceneWithMinimumWait(
            string sceneName,
            float minimumSeconds,
            Action<bool> onFinished)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (operation == null)
            {
                onFinished?.Invoke(false);
                yield break;
            }

            operation.allowSceneActivation = false;

            float elapsed = 0f;
            float minWait = Mathf.Max(0f, minimumSeconds);
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

            onFinished?.Invoke(true);
        }

        public static IEnumerator LoadMainMenuScene(string mainMenuSceneName, Action<bool> onFinished)
        {
            AsyncOperation loadMenu = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);

            if (loadMenu == null)
            {
                onFinished?.Invoke(false);
                yield break;
            }

            while (!loadMenu.isDone)
            {
                yield return null;
            }

            onFinished?.Invoke(true);
        }
    }
}
