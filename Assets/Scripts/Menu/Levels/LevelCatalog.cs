using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace Catsss.Menu.Levels
{
    /// <summary>Каталог уровней для экрана выбора (хост выбирает сцену перед стартом).</summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Catsss/Menu/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField]
        private LevelDefinition[] levels = Array.Empty<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> OrderedLevels =>
            levels == null || levels.Length == 0
                ? Array.Empty<LevelDefinition>()
                : levels.OrderBy(level => level.SortOrder).ThenBy(level => level.SceneName).ToArray();

        public bool TryGetLevel(int index, out LevelDefinition level)
        {
            IReadOnlyList<LevelDefinition> ordered = OrderedLevels;

            if (index < 0 || index >= ordered.Count)
            {
                level = null;
                return false;
            }

            level = ordered[index];
            return true;
        }

        public int Count => OrderedLevels.Count;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (levels == null)
            {
                return;
            }

            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            foreach (LevelDefinition level in levels)
            {
                if (level == null || string.IsNullOrWhiteSpace(level.SceneName))
                {
                    continue;
                }

                bool inBuild = false;

                foreach (string path in scenePaths)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                    if (fileName == level.SceneName)
                    {
                        inBuild = true;
                        break;
                    }
                }

                if (!inBuild)
                {
                    Debug.LogWarning(
                        $"[LevelCatalog] Сцена '{level.SceneName}' не найдена в Build Profiles. " +
                        "File → Build Profiles → добавь сцену или исправь Scene Name.",
                        this);
                }
            }
        }
#endif
    }
}
