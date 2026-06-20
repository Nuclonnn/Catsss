#if UNITY_EDITOR
using Catsss.Menu.Levels;
using UnityEditor;
using UnityEngine;

namespace Catsss.EditorTools
{
    public static class LevelCatalogSetupMenu
    {
        private const string CatalogPath = "Assets/Configs/LevelCatalog.asset";

        [MenuItem("Catsss/Menu/Create Default Level Catalog")]
        public static void CreateDefaultLevelCatalog()
        {
            LevelCatalog existing = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);

            if (existing != null)
            {
                Debug.Log($"[Catsss] LevelCatalog уже существует: {CatalogPath}");
                Selection.activeObject = existing;
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Configs"))
            {
                AssetDatabase.CreateFolder("Assets", "Configs");
            }

            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty levels = serialized.FindProperty("levels");
            levels.arraySize = 2;

            WriteLevel(levels.GetArrayElementAtIndex(0), "Sandbox", "level.sandbox.name", true, 0);
            WriteLevel(levels.GetArrayElementAtIndex(1), "Sanbox_1", "level.level02.name", false, 1);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Catsss] Создан {CatalogPath}. Добавь thumbnail и сцену Level_02 в Build Settings.");
            Selection.activeObject = catalog;
        }

        private static void WriteLevel(
            SerializedProperty element,
            string sceneName,
            string displayNameKey,
            bool isUnlocked,
            int sortOrder)
        {
            element.FindPropertyRelative("sceneName").stringValue = sceneName;
            element.FindPropertyRelative("displayNameKey").stringValue = displayNameKey;
            element.FindPropertyRelative("thumbnail").objectReferenceValue = null;
            element.FindPropertyRelative("isUnlocked").boolValue = isUnlocked;
            element.FindPropertyRelative("sortOrder").intValue = sortOrder;
        }
    }
}
#endif
