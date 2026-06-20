#if UNITY_EDITOR
using Catsss.Menu;
using Catsss.Menu.Levels;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Catsss.EditorTools
{
    /// <summary>Создаёт LevelSelectPanel на MainMenu и связывает с MainMenuController.</summary>
    public static class MainMenuLevelSelectSetupMenu
    {
        private const string MenuPath = "Catsss/Menu/Setup Level Select Panel UI";
        private const string CatalogPath = "Assets/Configs/LevelCatalog.asset";

        [MenuItem(MenuPath)]
        public static void SetupLevelSelectPanelUi()
        {
            LevelCatalogSetupMenu.CreateDefaultLevelCatalog();

            Scene scene = EditorSceneManager.OpenScene(LocalizationSetupMenu.MainMenuScenePath, OpenSceneMode.Single);
            Transform canvas = GameObject.Find("Canvas")?.transform;

            if (canvas == null)
            {
                Debug.LogError("[Catsss] Canvas не найден.");
                return;
            }

            Transform styleButton = canvas.Find("MainMenu/GuestButton");
            GameObject panel = EnsureLevelSelectPanel(canvas, styleButton);
            WireController(panel, canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Catsss] LevelSelectPanel создан. Запусти Setup UI Strings + Setup MainMenu Scene Texts.");
        }

        private static GameObject EnsureLevelSelectPanel(Transform canvas, Transform styleButton)
        {
            Transform existing = canvas.Find("LevelSelectPanel");

            if (existing != null)
            {
                return existing.gameObject;
            }

            Transform guestPanel = canvas.Find("GuestPanel");

            if (guestPanel == null)
            {
                Debug.LogError("[Catsss] GuestPanel не найден.");
                return null;
            }

            GameObject panel = Object.Instantiate(guestPanel.gameObject, canvas);
            panel.name = "LevelSelectPanel";
            panel.SetActive(false);

            DestroyImmediateSafe(panel.transform.Find("InputFieldIP")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("InputFieldPort")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("ConnectButton")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("ConnectionErrorText")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("BackButton")?.gameObject);

            Transform title = panel.transform.Find("Title");

            if (title != null && title.TryGetComponent(out TMP_Text titleTmp))
            {
                titleTmp.text = "Levels";
            }

            CreateSketchButton(panel.transform, "BackButton", styleButton, new Vector2(0f, 0f), new Vector2(250f, 100f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), "Back");
            CreateSketchButton(panel.transform, "PrevButton", styleButton, new Vector2(-360f, 0f), new Vector2(120f, 100f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "<");
            CreateSketchButton(panel.transform, "NextButton", styleButton, new Vector2(360f, 0f), new Vector2(120f, 100f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ">");
            CreateSketchButton(panel.transform, "PlayButton", styleButton, new Vector2(0f, -220f), new Vector2(320f, 100f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Play");

            GameObject card = CreateCard(panel.transform);
            CreateDots(panel.transform);
            CreateLockedHint(panel.transform);

            LevelSelectController controller = panel.GetComponent<LevelSelectController>();

            if (controller == null)
            {
                controller = panel.AddComponent<LevelSelectController>();
            }

            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("levelCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("thumbnailImage").objectReferenceValue = card.transform.Find("Thumbnail")?.GetComponent<Image>();
            serialized.FindProperty("levelNameTmp").objectReferenceValue = card.transform.Find("LevelName")?.GetComponent<TMP_Text>();
            serialized.FindProperty("lockOverlayRoot").objectReferenceValue = card.transform.Find("LockOverlay")?.gameObject;
            serialized.FindProperty("cardCanvasGroup").objectReferenceValue = card.GetComponent<CanvasGroup>();
            serialized.FindProperty("previousButton").objectReferenceValue = panel.transform.Find("PrevButton")?.GetComponent<Button>();
            serialized.FindProperty("nextButton").objectReferenceValue = panel.transform.Find("NextButton")?.GetComponent<Button>();
            serialized.FindProperty("dotsRoot").objectReferenceValue = panel.transform.Find("DotsRoot");
            serialized.FindProperty("dotPrefab").objectReferenceValue = panel.transform.Find("DotsRoot/DotTemplate")?.GetComponent<Image>();
            serialized.FindProperty("playButton").objectReferenceValue = panel.transform.Find("PlayButton")?.GetComponent<Button>();
            serialized.FindProperty("backButton").objectReferenceValue = panel.transform.Find("BackButton")?.GetComponent<Button>();
            serialized.FindProperty("lockedHintTmp").objectReferenceValue = panel.transform.Find("LockedHint")?.GetComponent<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static GameObject CreateCard(Transform parent)
        {
            var card = new GameObject("LevelCard", typeof(RectTransform), typeof(CanvasGroup));
            card.transform.SetParent(parent, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(520f, 320f);

            var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbnailObject.transform.SetParent(card.transform, false);
            RectTransform thumbRect = thumbnailObject.GetComponent<RectTransform>();
            thumbRect.anchorMin = Vector2.zero;
            thumbRect.anchorMax = Vector2.one;
            thumbRect.offsetMin = Vector2.zero;
            thumbRect.offsetMax = Vector2.zero;
            Image thumbImage = thumbnailObject.GetComponent<Image>();
            thumbImage.color = new Color(0.85f, 0.8f, 0.72f, 1f);
            thumbImage.preserveAspect = true;

            var nameObject = new GameObject("LevelName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(card.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, -36f);
            nameRect.sizeDelta = new Vector2(480f, 56f);
            TMP_Text nameTmp = nameObject.GetComponent<TextMeshProUGUI>();
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontSize = 34f;
            nameTmp.color = new Color(0.15f, 0.12f, 0.08f, 1f);

            var lockOverlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            lockOverlay.transform.SetParent(card.transform, false);
            RectTransform lockRect = lockOverlay.GetComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;
            Image lockImage = lockOverlay.GetComponent<Image>();
            lockImage.color = new Color(0.1f, 0.1f, 0.1f, 0.45f);
            lockOverlay.SetActive(false);

            return card;
        }

        private static void CreateDots(Transform parent)
        {
            var dotsRoot = new GameObject("DotsRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotsRoot.transform.SetParent(parent, false);
            RectTransform dotsRect = dotsRoot.GetComponent<RectTransform>();
            dotsRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotsRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotsRect.pivot = new Vector2(0.5f, 0.5f);
            dotsRect.anchoredPosition = new Vector2(0f, -150f);
            dotsRect.sizeDelta = new Vector2(240f, 24f);

            HorizontalLayoutGroup layout = dotsRoot.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var dotTemplate = new GameObject("DotTemplate", typeof(RectTransform), typeof(Image));
            dotTemplate.transform.SetParent(dotsRoot.transform, false);
            RectTransform dotRect = dotTemplate.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(14f, 14f);
            Image dotImage = dotTemplate.GetComponent<Image>();
            dotImage.color = new Color(0.15f, 0.12f, 0.08f, 0.35f);
            dotTemplate.SetActive(false);
        }

        private static void CreateLockedHint(Transform parent)
        {
            var hintObject = new GameObject("LockedHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObject.transform.SetParent(parent, false);
            RectTransform rect = hintObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -180f);
            rect.sizeDelta = new Vector2(500f, 40f);
            TMP_Text tmp = hintObject.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24f;
            tmp.color = new Color(0.35f, 0.28f, 0.18f, 1f);
            tmp.text = "Locked";
            hintObject.SetActive(false);
        }

        private static void CreateSketchButton(
            Transform parent,
            string name,
            Transform styleSource,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            string label)
        {
            Transform existing = parent.Find(name);

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            if (styleSource == null)
            {
                return;
            }

            GameObject clone = Object.Instantiate(styleSource.gameObject, parent);
            clone.name = name;
            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TMP_Text tmp = clone.GetComponentInChildren<TMP_Text>(true);

            if (tmp != null)
            {
                tmp.text = label;
            }
        }

        private static void WireController(GameObject panel, Transform canvas)
        {
            if (panel == null)
            {
                return;
            }

            GameObject mainMenuRoot = canvas.Find("MainMenu")?.gameObject;

            if (mainMenuRoot == null || !mainMenuRoot.TryGetComponent(out MainMenuController controller))
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("levelSelectPanelRoot").objectReferenceValue = panel;
            serialized.FindProperty("levelSelectController").objectReferenceValue = panel.GetComponent<LevelSelectController>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void DestroyImmediateSafe(GameObject target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
