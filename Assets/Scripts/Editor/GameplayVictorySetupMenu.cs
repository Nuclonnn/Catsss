#if UNITY_EDITOR
using Catsss.Core.Localization;
using Catsss.Gameplay;
using Catsss.Menu;
using Catsss.Menu.Levels;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Catsss.EditorTools
{
    /// <summary>
    /// Создаёт victory overlay на PlayerRoot.prefab и связывает GameplayVictoryController.
    /// </summary>
    public static class GameplayVictorySetupMenu
    {
        private const string MenuPath = "Catsss/Gameplay/Setup Victory Overlay UI";
        private const string LocalizationMenuPath = "Catsss/Localization/Setup Victory Overlay Texts";
        private const string PlayerRootPrefabPath = "Assets/Prefabs/PlayerRoot.prefab";
        private const string LevelCatalogPath = "Assets/Configs/LevelCatalog.asset";

        private static readonly (string RelativePath, string Key, string EnglishFallback)[] VictoryTextBindings =
        {
            ("VictoryOverlay/VictoryCanvas/VictoryMenuPanel/Title", "victory.title", "Congratulations, you passed level"),
            ("VictoryOverlay/VictoryCanvas/VictoryMenuPanel/MenuButton/Text (TMP)", "victory.menu", "Menu")
        };

        [MenuItem(MenuPath)]
        public static void SetupVictoryOverlayUi()
        {
            LocalizationSetupMenu.SetupUiStrings();

            Transform styleButton = TryFindMainMenuButtonTemplate();

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                EnsureVictoryHierarchy(prefabRoot.transform, styleButton);
                WireGameplayVictoryController(prefabRoot);
                WireVictoryLocalization(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerRootPrefabPath);
                Debug.Log("[Catsss] Victory overlay создан на PlayerRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem(LocalizationMenuPath)]
        public static void SetupVictoryOverlayTexts()
        {
            LocalizationSetupMenu.SetupUiStrings();

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                int wired = WireVictoryLocalization(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerRootPrefabPath);
                Debug.Log($"[Catsss] Victory overlay localization: {wired} текстов.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform TryFindMainMenuButtonTemplate()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.path != LocalizationSetupMenu.MainMenuScenePath || !scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Transform guestButton = root.transform.Find("Canvas/MainMenu/GuestButton");

                    if (guestButton != null)
                    {
                        return guestButton;
                    }
                }
            }

            return null;
        }

        private static void EnsureVictoryHierarchy(Transform playerRoot, Transform styleButton)
        {
            Transform existing = playerRoot.Find("VictoryOverlay");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var overlayRoot = new GameObject("VictoryOverlay");
            overlayRoot.transform.SetParent(playerRoot, false);
            overlayRoot.SetActive(false);

            GameObject canvasObject = CreateVictoryCanvas(overlayRoot.transform);
            Transform canvas = canvasObject.transform;

            CreateDimmer(canvas);
            CreateVictoryMenuPanel(canvas, styleButton);
        }

        private static GameObject CreateVictoryCanvas(Transform parent)
        {
            var canvasObject = new GameObject(
                "VictoryCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210; // Выше паузы

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(canvasObject.GetComponent<RectTransform>());
            return canvasObject;
        }

        private static void CreateDimmer(Transform canvas)
        {
            var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimmer.transform.SetParent(canvas, false);
            Stretch(dimmer.GetComponent<RectTransform>());
            Image image = dimmer.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);
            image.raycastTarget = true;
        }

        private static GameObject CreateVictoryMenuPanel(Transform canvas, Transform styleButton)
        {
            var panel = new GameObject("VictoryMenuPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            Stretch(panel.GetComponent<RectTransform>());

            CreateCenteredLabel(panel.transform, "Title", "Congratulations, you passed level", new Vector2(0f, 180f), 48f, 800f);
            CreateCenteredLabel(panel.transform, "LevelNameText", "Level Name", new Vector2(0f, 100f), 64f, 800f);
            
            // Добавляем LocalizedUiText для LevelNameText, но без конкретного ключа (он будет задан в рантайме)
            Transform levelNameTextTransform = panel.transform.Find("LevelNameText");
            if (levelNameTextTransform != null)
            {
                LocalizedUiText localized = levelNameTextTransform.gameObject.AddComponent<LocalizedUiText>();
                StringTableCollection collection = GetUiStringsCollection();
                AssignLocalizedReference(localized, collection, "level.sandbox.name", "Level Name");
            }

            CreateMenuButton(panel.transform, styleButton, "MenuButton", "Menu", new Vector2(0f, -60f));

            return panel;
        }

        private static void WireGameplayVictoryController(GameObject playerRoot)
        {
            GameplayVictoryController controller = playerRoot.GetComponent<GameplayVictoryController>();

            if (controller == null)
            {
                controller = playerRoot.AddComponent<GameplayVictoryController>();
            }

            Transform overlay = playerRoot.transform.Find("VictoryOverlay");
            Transform canvas = overlay?.Find("VictoryCanvas");
            Transform victoryMenu = canvas?.Find("VictoryMenuPanel");

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("inputReader").objectReferenceValue =
                playerRoot.GetComponent<Catsss.Player.PlayerInputReader>();
            serialized.FindProperty("cameraController").objectReferenceValue =
                playerRoot.GetComponentInChildren<Catsss.Player.PlayerCameraController>(true);
            
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelCatalogPath);
            serialized.FindProperty("levelCatalog").objectReferenceValue = catalog;

            serialized.FindProperty("victoryOverlayRoot").objectReferenceValue = overlay?.gameObject;
            serialized.FindProperty("levelNameText").objectReferenceValue = 
                victoryMenu?.Find("LevelNameText")?.GetComponent<LocalizedUiText>();
            serialized.FindProperty("menuButton").objectReferenceValue =
                victoryMenu?.Find("MenuButton")?.GetComponent<Button>();
            
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static int WireVictoryLocalization(Transform playerRoot)
        {
            StringTableCollection collection = GetUiStringsCollection();
            int wired = 0;

            foreach ((string relativePath, string key, string englishFallback) in VictoryTextBindings)
            {
                Transform target = playerRoot.Find(relativePath);

                if (target == null)
                {
                    Debug.LogWarning($"[Catsss] Victory overlay: не найден '{relativePath}'.");
                    continue;
                }

                LocalizedUiText localized = target.GetComponent<LocalizedUiText>();

                if (localized == null)
                {
                    localized = target.gameObject.AddComponent<LocalizedUiText>();
                }

                AssignLocalizedReference(localized, collection, key, englishFallback);
                wired++;
            }

            return wired;
        }

        private static StringTableCollection GetUiStringsCollection()
        {
            foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (collection.name == "UI_Strings")
                {
                    return collection;
                }
            }

            return null;
        }

        private static void AssignLocalizedReference(
            LocalizedUiText component,
            StringTableCollection collection,
            string key,
            string englishFallback)
        {
            if (collection == null)
            {
                return;
            }

            SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(key);

            if (sharedEntry == null)
            {
                Debug.LogWarning($"[Catsss] UI_Strings has no key '{key}'.");
                return;
            }

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty rootProperty = serialized.FindProperty("textReference");
            SerializedProperty localizedText = rootProperty.FindPropertyRelative("localizedText");
            SerializedProperty tableReference = localizedText.FindPropertyRelative("m_TableReference");
            tableReference.FindPropertyRelative("m_TableCollectionName").stringValue =
                collection.TableCollectionNameReference.TableCollectionName;

            SerializedProperty entryReference = localizedText.FindPropertyRelative("m_TableEntryReference");
            entryReference.FindPropertyRelative("m_KeyId").longValue = sharedEntry.Id;
            entryReference.FindPropertyRelative("m_Key").stringValue = key;
            rootProperty.FindPropertyRelative("editorFallback").stringValue = englishFallback;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static void CreateCenteredLabel(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            float fontSize,
            float width = 520f)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 70f);
            TMP_Text tmp = labelObject.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.95f, 0.9f, 0.82f, 1f);
            tmp.text = text;
        }

        private static void CreateMenuButton(
            Transform parent,
            Transform styleButton,
            string name,
            string label,
            Vector2 position,
            Vector2? size = null)
        {
            Vector2 buttonSize = size ?? new Vector2(360f, 90f);
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = buttonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            if (styleButton != null && styleButton.TryGetComponent(out Image styleImage))
            {
                image.sprite = styleImage.sprite;
                image.type = styleImage.type;
                image.pixelsPerUnitMultiplier = styleImage.pixelsPerUnitMultiplier;
                image.material = styleImage.material;
            }
            else
            {
                image.color = new Color(0.85f, 0.78f, 0.62f, 1f);
            }

            var textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            Stretch(textObject.GetComponent<RectTransform>());
            TMP_Text tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 30f;
            tmp.color = new Color(0.15f, 0.12f, 0.08f, 1f);

            if (styleButton != null)
            {
                TMP_Text styleText = styleButton.GetComponentInChildren<TMP_Text>(true);

                if (styleText != null)
                {
                    tmp.font = styleText.font;
                    tmp.fontSharedMaterial = styleText.fontSharedMaterial;
                }
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
