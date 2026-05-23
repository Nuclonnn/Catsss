using UnityEngine;

namespace Catsss.Menu
{
    /// <summary>
    /// Завершение игры / выход из Play Mode редактора (как Helper в ScriptsForReference).
    /// </summary>
    public static class ApplicationLifecycle
    {
        public static void QuitOrExitPlayMode()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
