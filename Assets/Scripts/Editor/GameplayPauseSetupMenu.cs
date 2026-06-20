#if UNITY_EDITOR
using Catsss.Core.Localization;
using Catsss.Gameplay;
using Catsss.Menu;
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
    /// Создаёт pause overlay на PlayerRoot.prefab и связывает GameplayPauseController.
    /// </summary>
    public static class GameplayPauseSetupMenu
    {
        private const string MenuPath = "Catsss/Gameplay/Setup Pause Overlay UI";
        private const string LocalizationMenuPath = "Catsss/Localization/Setup Pause Overlay Texts";
        private const string PlayerRootPrefabPath = "Assets/Prefabs/PlayerRoot.prefab";

        private static readonly (string RelativePath, string Key, string EnglishFallback)[] PauseTextBindings =
        {
            ("PauseOverlay/PauseCanvas/PauseMenuPanel/Title", "pause.title", "Paused"),
            ("PauseOverlay/PauseCanvas/PauseMenuPanel/ResumeButton/Text (TMP)", "pause.resume", "Resume"),
            ("PauseOverlay/PauseCanvas/PauseMenuPanel/SettingsButton/Text (TMP)", "pause.settings", "Settings"),
            ("PauseOverlay/PauseCanvas/PauseMenuPanel/ExitSessionButton/Text (TMP)", "pause.exit_session", "Exit to menu"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/Title", "settings.title", "Settings"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/BackButton/Text (TMP)", "menu.button.back", "Back"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/SensitivityLabel", "settings.sensitivity", "Mouse sensitivity"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/LanguageLabel", "settings.language", "Language"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/EnglishButton/Text (TMP)", "settings.language.english", "English"),
            ("PauseOverlay/PauseCanvas/PauseSettingsPanel/RussianButton/Text (TMP)", "settings.language.russian", "Russian"),
        };

        [MenuItem(MenuPath)]
        public static void SetupPauseOverlayUi()
        {
            LocalizationSetupMenu.SetupUiStrings();

            // Стиль кнопок берём только если MainMenu уже открыт в редакторе — без переключения сцен.
            Transform styleButton = TryFindMainMenuButtonTemplate();

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                EnsurePauseHierarchy(prefabRoot.transform, styleButton);
                WireGameplayPauseController(prefabRoot);
                WirePauseLocalization(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerRootPrefabPath);
                Debug.Log("[Catsss] Pause overlay создан на PlayerRoot. Запусти Play и нажми Esc в геймплее.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem(LocalizationMenuPath)]
        public static void SetupPauseOverlayTexts()
        {
            LocalizationSetupMenu.SetupUiStrings();

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                int wired = WirePauseLocalization(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerRootPrefabPath);
                Debug.Log($"[Catsss] Pause overlay localization: {wired} текстов.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Ищет шаблон кнопки на уже открытой сцене MainMenu. Не открывает и не закрывает сцены.
        /// </summary>
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

        private static void EnsurePauseHierarchy(Transform playerRoot, Transform styleButton)
        {
            Transform existing = playerRoot.Find("PauseOverlay");

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var overlayRoot = new GameObject("PauseOverlay");
            overlayRoot.transform.SetParent(playerRoot, false);
            overlayRoot.SetActive(false);

            GameObject canvasObject = CreatePauseCanvas(overlayRoot.transform);
            Transform canvas = canvasObject.transform;

            CreateDimmer(canvas);
            GameObject pauseMenu = CreatePauseMenuPanel(canvas, styleButton);
            GameObject settingsPanel = CreatePauseSettingsPanel(canvas, styleButton);
            settingsPanel.SetActive(false);
        }

        private static GameObject CreatePauseCanvas(Transform parent)
        {
            var canvasObject = new GameObject(
                "PauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

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
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = true;
        }

        private static GameObject CreatePauseMenuPanel(Transform canvas, Transform styleButton)
        {
            var panel = new GameObject("PauseMenuPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            Stretch(panel.GetComponent<RectTransform>());

            CreateCenteredLabel(panel.transform, "Title", "Paused", new Vector2(0f, 180f), 48f);
            CreateMenuButton(panel.transform, styleButton, "ResumeButton", "Resume", new Vector2(0f, 40f));
            CreateMenuButton(panel.transform, styleButton, "SettingsButton", "Settings", new Vector2(0f, -60f));
            CreateMenuButton(panel.transform, styleButton, "ExitSessionButton", "Exit to menu", new Vector2(0f, -160f));

            return panel;
        }

        private static GameObject CreatePauseSettingsPanel(Transform canvas, Transform styleButton)
        {
            var panel = new GameObject("PauseSettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            Stretch(panel.GetComponent<RectTransform>());

            CreateCenteredLabel(panel.transform, "Title", "Settings", new Vector2(0f, 200f), 42f);
            CreateCenteredLabel(panel.transform, "SensitivityLabel", "Mouse sensitivity", new Vector2(0f, 90f), 30f);
            CreateSlider(panel.transform, "SensitivitySlider", new Vector2(0f, 40f));
            CreateCenteredLabel(panel.transform, "SensitivityValue", "1.00", new Vector2(300f, 40f), 28f, 100f);
            CreateCenteredLabel(panel.transform, "LanguageLabel", "Language", new Vector2(0f, -40f), 30f);
            CreateMenuButton(panel.transform, styleButton, "EnglishButton", "English", new Vector2(-140f, -110f), new Vector2(240f, 90f));
            CreateMenuButton(panel.transform, styleButton, "RussianButton", "Russian", new Vector2(140f, -110f), new Vector2(240f, 90f));
            CreateMenuButton(panel.transform, styleButton, "BackButton", "Back", new Vector2(0f, -220f), new Vector2(250f, 90f));

            SettingsPanelController controller = panel.AddComponent<SettingsPanelController>();
            WireSettingsPanelController(panel, controller);
            return panel;
        }

        private static void WireSettingsPanelController(GameObject settingsPanel, SettingsPanelController controller)
        {
            Transform sensitivityValue = settingsPanel.transform.Find("SensitivityValue");
            TMP_InputField inputField = sensitivityValue?.GetComponent<TMP_InputField>();

            if (inputField == null && sensitivityValue != null)
            {
                inputField = sensitivityValue.gameObject.AddComponent<TMP_InputField>();
                TMP_Text text = sensitivityValue.GetComponent<TextMeshProUGUI>();

                if (text == null)
                {
                    text = sensitivityValue.gameObject.AddComponent<TextMeshProUGUI>();
                }

                text.raycastTarget = true;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 28f;
                inputField.textComponent = text;
                inputField.textViewport = text.rectTransform;
                inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                inputField.lineType = TMP_InputField.LineType.SingleLine;
                inputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("mouseSensitivitySlider").objectReferenceValue =
                settingsPanel.transform.Find("SensitivitySlider")?.GetComponent<Slider>();
            serialized.FindProperty("sensitivityInputField").objectReferenceValue = inputField;
            serialized.FindProperty("sensitivityValueTmp").objectReferenceValue =
                inputField == null ? sensitivityValue?.GetComponent<TMP_Text>() : null;
            serialized.FindProperty("englishButton").objectReferenceValue =
                settingsPanel.transform.Find("EnglishButton")?.GetComponent<Button>();
            serialized.FindProperty("russianButton").objectReferenceValue =
                settingsPanel.transform.Find("RussianButton")?.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGameplayPauseController(GameObject playerRoot)
        {
            GameplayPauseController controller = playerRoot.GetComponent<GameplayPauseController>();

            if (controller == null)
            {
                controller = playerRoot.AddComponent<GameplayPauseController>();
            }

            Transform overlay = playerRoot.transform.Find("PauseOverlay");
            Transform canvas = overlay?.Find("PauseCanvas");
            Transform pauseMenu = canvas?.Find("PauseMenuPanel");
            Transform settings = canvas?.Find("PauseSettingsPanel");

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("inputReader").objectReferenceValue =
                playerRoot.GetComponent<Catsss.Player.PlayerInputReader>();
            serialized.FindProperty("cameraController").objectReferenceValue =
                playerRoot.GetComponentInChildren<Catsss.Player.PlayerCameraController>(true);
            serialized.FindProperty("pauseOverlayRoot").objectReferenceValue = overlay?.gameObject;
            serialized.FindProperty("pauseMenuPanel").objectReferenceValue = pauseMenu?.gameObject;
            serialized.FindProperty("settingsPanel").objectReferenceValue = settings?.gameObject;
            serialized.FindProperty("resumeButton").objectReferenceValue =
                pauseMenu?.Find("ResumeButton")?.GetComponent<Button>();
            serialized.FindProperty("settingsButton").objectReferenceValue =
                pauseMenu?.Find("SettingsButton")?.GetComponent<Button>();
            serialized.FindProperty("exitSessionButton").objectReferenceValue =
                pauseMenu?.Find("ExitSessionButton")?.GetComponent<Button>();
            serialized.FindProperty("settingsBackButton").objectReferenceValue =
                settings?.Find("BackButton")?.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static int WirePauseLocalization(Transform playerRoot)
        {
            StringTableCollection collection = GetUiStringsCollection();
            int wired = 0;

            foreach ((string relativePath, string key, string englishFallback) in PauseTextBindings)
            {
                Transform target = playerRoot.Find(relativePath);

                if (target == null)
                {
                    Debug.LogWarning($"[Catsss] Pause overlay: не найден '{relativePath}'.");
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
            rect.sizeDelta = new Vector2(width, 56f);
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

        private static void CreateSlider(Transform parent, string name, Vector2 position)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(420f, 30f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0.25f;
            slider.maxValue = 2f;
            slider.value = 1f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.28f, 0.22f, 0.16f, 0.85f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.34f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handle.GetComponent<Image>().color = new Color(0.93f, 0.88f, 0.78f, 1f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
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
