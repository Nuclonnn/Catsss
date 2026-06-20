using UnityEngine;

namespace Catsss.Menu
{
    /// <summary>
    /// Data-driven настройки MainMenu: порт хоста, fallback-сцена для guest connect, таймауты и overlay загрузки.
    /// </summary>
    [CreateAssetMenu(fileName = "MenuConfig", menuName = "Catsss/Menu/Menu Config")]
    public sealed class MenuConfig : ScriptableObject
    {
        [Header("Networking")]
        [Tooltip("Сцена для guest connect, если уровень не выбирается (хост берёт сцену из Level Catalog).")]
        [SerializeField]
        private string defaultGameplaySceneName = NetworkSessionIntent.DefaultLevelSceneName;

        [SerializeField]
        private ushort defaultHostPort = NetworkSessionIntent.DefaultPort;

        [Header("Loading")]
        [SerializeField]
        private float minimumSecondsLoadingScreen = 0.35f;

        [SerializeField]
        private float overlayHoldSecondsAfterConnect = 0.5f;

        [SerializeField, Min(1f)]
        [Tooltip("Сколько секунд гость ждёт ответа хоста перед возвратом в меню.")]
        private float clientConnectTimeoutSeconds = 20f;

        [SerializeField]
        private Color loadingBackdropColorWithoutSprite = Color.black;

        [SerializeField]
        private Sprite loadingSplashSpriteOptional;

        public string DefaultGameplaySceneName => defaultGameplaySceneName;

        public ushort DefaultHostPort => defaultHostPort;

        public MenuLoadPresentation ToLoadPresentation()
        {
            return new MenuLoadPresentation
            {
                MinimumSecondsLoadingScreen = minimumSecondsLoadingScreen,
                OverlayHoldSecondsAfterConnect = overlayHoldSecondsAfterConnect,
                ClientConnectTimeoutSeconds = clientConnectTimeoutSeconds,
                LoadingBackdropColorWithoutSprite = loadingBackdropColorWithoutSprite,
                LoadingSplashSpriteOptional = loadingSplashSpriteOptional,
            };
        }
    }
}
