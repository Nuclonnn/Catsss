using System;
using Catsss.Menu;
using UnityEngine;

namespace Catsss.Menu.Levels
{
    /// <summary>Один уровень в каталоге выбора: сцена, локализованное имя, превью, lock.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField]
        private string sceneName = NetworkSessionIntent.DefaultLevelSceneName;

        [SerializeField]
        private string displayNameKey = "level.sandbox.name";

        [SerializeField]
        private Sprite thumbnail;

        [SerializeField]
        private bool isUnlocked = true;

        [SerializeField]
        private int sortOrder;

        public string SceneName => string.IsNullOrWhiteSpace(sceneName)
            ? NetworkSessionIntent.DefaultLevelSceneName
            : sceneName.Trim();

        public string DisplayNameKey => displayNameKey ?? string.Empty;

        public Sprite Thumbnail => thumbnail;

        public bool IsUnlocked => isUnlocked;

        public int SortOrder => sortOrder;
    }
}
