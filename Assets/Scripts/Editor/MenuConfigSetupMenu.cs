#if UNITY_EDITOR
using Catsss.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Catsss.EditorTools
{
    /// <summary>Создаёт MenuConfig asset и назначает его на MainMenuController.</summary>
    public static class MenuConfigSetupMenu
    {
        private const string ConfigPath = "Assets/Configs/MenuConfig.asset";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Catsss/Menu/Create Default Menu Config")]
        public static void CreateDefaultMenuConfig()
        {
            MenuConfig config = EnsureMenuConfigAsset();
            WireMainMenuController(config);
            Debug.Log($"[Catsss] MenuConfig готов: {ConfigPath}. Проверь значения в Inspector.");
        }

        private static MenuConfig EnsureMenuConfigAsset()
        {
            MenuConfig existing = AssetDatabase.LoadAssetAtPath<MenuConfig>(ConfigPath);

            if (existing != null)
            {
                EnsureDefaultSplashAssigned(existing);
                Selection.activeObject = existing;
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }

            var config = ScriptableObject.CreateInstance<MenuConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();

            SerializedObject serialized = new SerializedObject(config);
            Sprite defaultSplash = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Background main menu.png");

            if (defaultSplash != null)
            {
                serialized.FindProperty("loadingSplashSpriteOptional").objectReferenceValue = defaultSplash;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            return config;
        }

        private static void EnsureDefaultSplashAssigned(MenuConfig config)
        {
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty splash = serialized.FindProperty("loadingSplashSpriteOptional");

            if (splash.objectReferenceValue != null)
            {
                return;
            }

            Sprite defaultSplash = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Background main menu.png");

            if (defaultSplash == null)
            {
                return;
            }

            splash.objectReferenceValue = defaultSplash;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static void WireMainMenuController(MenuConfig config)
        {
            if (config == null)
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            GameObject mainMenuRoot = GameObject.Find("MainMenu");

            if (mainMenuRoot == null || !mainMenuRoot.TryGetComponent(out MainMenuController controller))
            {
                Debug.LogWarning("[Catsss] MainMenuController не найден — назначь MenuConfig вручную на сцене MainMenu.");
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("menuConfig").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
