using System;
using UnityEngine;

namespace Catsss.Menu
{
    /// <summary>Параметры экрана загрузки при переходе из меню в геймплей. Fallback — когда <see cref="MenuConfig"/> не назначен.</summary>
    [Serializable]
    public struct MenuLoadPresentation
    {
        public float MinimumSecondsLoadingScreen;
        public float OverlayHoldSecondsAfterConnect;
        public float ClientConnectTimeoutSeconds;
        public Color LoadingBackdropColorWithoutSprite;
        public Sprite LoadingSplashSpriteOptional;

        public static MenuLoadPresentation Default =>
            new()
            {
                MinimumSecondsLoadingScreen = 0.35f,
                OverlayHoldSecondsAfterConnect = 0.5f,
                ClientConnectTimeoutSeconds = 20f,
                LoadingBackdropColorWithoutSprite = Color.black,
                LoadingSplashSpriteOptional = null,
            };
    }
}
