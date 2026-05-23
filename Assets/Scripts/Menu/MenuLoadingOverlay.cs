using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Catsss.Menu
{
    /// <summary>
    /// Полноэкранный блок на время LoadSceneAsync, чтобы скрыть пустой кадр. Живёт в DontDestroyOnLoad.
    /// </summary>
    public sealed class MenuLoadingOverlay : MonoBehaviour
    {
        public static MenuLoadingOverlay Instance { get; private set; }

        private Canvas _canvas;
        private GraphicRaycaster _raycaster;

        public Coroutine RunCoroutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public static MenuLoadingOverlay Create(Sprite fullscreenSprite, Color tintWhenNoSprite)
        {
            if (Instance != null)
            {
                Instance.Show();
                return Instance;
            }

            var root = new GameObject(nameof(MenuLoadingOverlay));
            DontDestroyOnLoad(root);
            var overlay = root.AddComponent<MenuLoadingOverlay>();
            overlay.Build(fullscreenSprite, tintWhenNoSprite);
            Instance = overlay;
            return overlay;
        }

        private void Build(Sprite fullscreenSprite, Color tintWhenNoSprite)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
            _raycaster = gameObject.AddComponent<GraphicRaycaster>();

            GameObject backdrop = new("Backdrop");
            backdrop.transform.SetParent(transform, worldPositionStays: false);
            RectTransform rt = backdrop.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            UnityEngine.UI.Image image = backdrop.AddComponent<UnityEngine.UI.Image>();

            if (fullscreenSprite != null)
            {
                image.sprite = fullscreenSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = tintWhenNoSprite;
            }
        }

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }

            if (_raycaster != null)
            {
                _raycaster.enabled = true;
            }
        }

        public void HideAndDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Destroy(gameObject);
        }
    }
}
