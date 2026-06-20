#if UNITY_EDITOR
using Catsss.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Catsss.EditorTools
{
    /// <summary>
    /// Создаёт SettingsPanel и кнопку Settings на MainMenu, если их ещё нет.
    /// </summary>
    public static class MainMenuSettingsSetupMenu
    {
        private const string MenuPath = "Catsss/Menu/Setup Settings Panel UI";
        private const string StyleMenuPath = "Catsss/Menu/Apply Settings Sketch Style";

        [MenuItem(StyleMenuPath)]
        public static void ApplySettingsSketchStyle()
        {
            Scene scene = EditorSceneManager.OpenScene(LocalizationSetupMenu.MainMenuScenePath, OpenSceneMode.Single);
            Transform canvas = GameObject.Find("Canvas")?.transform;

            if (canvas == null)
            {
                Debug.LogError("[Catsss] Canvas не найден на MainMenu.");
                return;
            }

            Transform settingsPanel = canvas.Find("SettingsPanel");
            Transform styleSource = canvas.Find("MainMenu/GuestButton") ?? canvas.Find("MainMenu/QuitButton");

            if (settingsPanel == null)
            {
                Debug.LogError("[Catsss] SettingsPanel не найден. Сначала запусти Setup Settings Panel UI.");
                return;
            }

            if (styleSource == null)
            {
                Debug.LogError("[Catsss] Не найдена кнопка-образец на MainMenu.");
                return;
            }

            ApplySketchButtonStyle(styleSource, settingsPanel.Find("EnglishButton"));
            ApplySketchButtonStyle(styleSource, settingsPanel.Find("RussianButton"));

            Transform english = settingsPanel.Find("EnglishButton");
            Transform russian = settingsPanel.Find("RussianButton");

            if (english != null)
            {
                SetRect(english, new Vector2(-140f, -150f), new Vector2(240f, 100f));
            }

            if (russian != null)
            {
                SetRect(russian, new Vector2(140f, -150f), new Vector2(240f, 100f));
            }

            ApplySketchSliderStyle(settingsPanel.Find("SensitivitySlider"));
            ApplySketchLabelStyle(settingsPanel, "SensitivityLabel");
            ApplySketchLabelStyle(settingsPanel, "LanguageLabel");
            TMP_InputField sensitivityInput = EnsureSensitivityInputField(settingsPanel);
            FixMainMenuControllerReferences(canvas);
            WireSettingsPanelController(settingsPanel.gameObject, sensitivityInput);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Catsss] Settings sketch style applied. Запусти Setup MainMenu Scene Texts, если ещё не делал.");
        }

        [MenuItem(MenuPath)]
        public static void SetupSettingsPanelUi()
        {
            Scene scene = EditorSceneManager.OpenScene(LocalizationSetupMenu.MainMenuScenePath, OpenSceneMode.Single);
            Transform canvas = GameObject.Find("Canvas")?.transform;

            if (canvas == null)
            {
                Debug.LogError("[Catsss] Canvas не найден на MainMenu.");
                return;
            }

            Transform mainMenu = canvas.Find("MainMenu");

            if (mainMenu == null)
            {
                Debug.LogError("[Catsss] MainMenu panel не найден.");
                return;
            }

            EnsureSettingsButton(mainMenu);
            GameObject settingsPanel = EnsureSettingsPanel(canvas);
            WireSettingsPanelController(settingsPanel, null);
            RepositionMainMenuButtons(mainMenu);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Catsss] Settings panel UI создан/обновлён. Запусти Setup MainMenu Scene Texts для локализации.");
        }

        private static void EnsureSettingsButton(Transform mainMenu)
        {
            if (mainMenu.Find("SettingsButton") != null)
            {
                return;
            }

            Transform quitButton = mainMenu.Find("QuitButton");
            Transform guestButton = mainMenu.Find("GuestButton");
            Transform template = guestButton != null ? guestButton : quitButton;

            if (template == null)
            {
                Debug.LogWarning("[Catsss] Не найден шаблон кнопки для SettingsButton.");
                return;
            }

            GameObject clone = Object.Instantiate(template.gameObject, mainMenu);
            clone.name = "SettingsButton";
            RectTransform rect = clone.GetComponent<RectTransform>();

            if (quitButton != null)
            {
                RectTransform quitRect = quitButton.GetComponent<RectTransform>();
                rect.anchoredPosition = quitRect.anchoredPosition + new Vector2(0f, 70f);
            }

            TMP_Text label = clone.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = "Settings";
            }
        }

        private static GameObject EnsureSettingsPanel(Transform canvas)
        {
            Transform existing = canvas.Find("SettingsPanel");

            if (existing != null)
            {
                return existing.gameObject;
            }

            Transform guestPanel = canvas.Find("GuestPanel");

            if (guestPanel == null)
            {
                Debug.LogError("[Catsss] GuestPanel не найден — нужен как шаблон.");
                return null;
            }

            GameObject panel = Object.Instantiate(guestPanel.gameObject, canvas);
            panel.name = "SettingsPanel";
            panel.SetActive(false);

            DestroyImmediateSafe(panel.transform.Find("InputFieldIP")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("InputFieldPort")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("ConnectButton")?.gameObject);
            DestroyImmediateSafe(panel.transform.Find("ConnectionErrorText")?.gameObject);

            Transform title = panel.transform.Find("Title");

            if (title != null && title.TryGetComponent(out TMP_Text titleTmp))
            {
                titleTmp.text = "Settings";
            }

            CreateLabel(panel.transform, "SensitivityLabel", "Mouse sensitivity", new Vector2(0f, 40f));
            CreateSlider(panel.transform, "SensitivitySlider", new Vector2(0f, -10f));
            CreateLabel(panel.transform, "SensitivityValue", "1.00", new Vector2(280f, -10f), 120f);
            CreateLabel(panel.transform, "LanguageLabel", "Language", new Vector2(0f, -90f));
            CreateLanguageButton(panel.transform, "EnglishButton", "English", new Vector2(-120f, -150f));
            CreateLanguageButton(panel.transform, "RussianButton", "Russian", new Vector2(120f, -150f));

            return panel;
        }

        private static void WireSettingsPanelController(GameObject settingsPanel, TMP_InputField sensitivityInputField)
        {
            if (settingsPanel == null)
            {
                return;
            }

            SettingsPanelController controller = settingsPanel.GetComponent<SettingsPanelController>();

            if (controller == null)
            {
                controller = settingsPanel.AddComponent<SettingsPanelController>();
            }

            Transform sensitivityValue = settingsPanel.transform.Find("SensitivityValue");
            TMP_InputField resolvedInput = sensitivityInputField;

            if (resolvedInput == null && sensitivityValue != null)
            {
                resolvedInput = sensitivityValue.GetComponent<TMP_InputField>();
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("mouseSensitivitySlider").objectReferenceValue =
                settingsPanel.transform.Find("SensitivitySlider")?.GetComponent<Slider>();
            serialized.FindProperty("sensitivityInputField").objectReferenceValue = resolvedInput;
            serialized.FindProperty("sensitivityValueTmp").objectReferenceValue =
                resolvedInput == null ? sensitivityValue?.GetComponent<TMP_Text>() : null;
            serialized.FindProperty("englishButton").objectReferenceValue =
                settingsPanel.transform.Find("EnglishButton")?.GetComponent<Button>();
            serialized.FindProperty("russianButton").objectReferenceValue =
                settingsPanel.transform.Find("RussianButton")?.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void ApplySketchButtonStyle(Transform source, Transform target)
        {
            if (source == null || target == null)
            {
                return;
            }

            if (!source.TryGetComponent(out Image sourceImage) || !target.TryGetComponent(out Image targetImage))
            {
                return;
            }

            targetImage.sprite = sourceImage.sprite;
            targetImage.type = sourceImage.type;
            targetImage.color = Color.white;
            targetImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            targetImage.material = sourceImage.material;

            TMP_Text sourceText = source.GetComponentInChildren<TMP_Text>(true);
            TMP_Text targetText = target.GetComponentInChildren<TMP_Text>(true);

            if (sourceText != null && targetText != null)
            {
                targetText.font = sourceText.font;
                targetText.fontSharedMaterial = sourceText.fontSharedMaterial;
                targetText.fontSize = sourceText.fontSize;
                targetText.color = sourceText.color;
                targetText.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void ApplySketchSliderStyle(Transform sliderTransform)
        {
            if (sliderTransform == null || !sliderTransform.TryGetComponent(out Slider slider))
            {
                return;
            }

            Color track = new(0.28f, 0.22f, 0.16f, 0.85f);
            Color fill = new(0.62f, 0.5f, 0.34f, 1f);
            Color handle = new(0.93f, 0.88f, 0.78f, 1f);

            ApplyChildImageColor(sliderTransform.Find("Background"), track);
            ApplyChildImageColor(sliderTransform.Find("Fill Area/Fill"), fill);
            ApplyChildImageColor(sliderTransform.Find("Handle Slide Area/Handle"), handle);
            slider.targetGraphic = sliderTransform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
        }

        private static void ApplySketchLabelStyle(Transform panel, string labelName)
        {
            Transform label = panel.Find(labelName);

            if (label != null && label.TryGetComponent(out TMP_Text tmp))
            {
                tmp.color = new Color(0.2f, 0.16f, 0.1f, 1f);
            }
        }

        private static void ApplyChildImageColor(Transform target, Color color)
        {
            if (target != null && target.TryGetComponent(out Image image))
            {
                image.color = color;
            }
        }

        private static TMP_InputField EnsureSensitivityInputField(Transform settingsPanel)
        {
            Transform valueTransform = settingsPanel.Find("SensitivityValue");

            if (valueTransform == null)
            {
                return null;
            }

            TMP_InputField inputField = valueTransform.GetComponent<TMP_InputField>();

            if (inputField == null)
            {
                inputField = valueTransform.gameObject.AddComponent<TMP_InputField>();
            }

            TMP_Text text = valueTransform.GetComponent<TextMeshProUGUI>();

            if (text == null)
            {
                text = valueTransform.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = true;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30f;
            text.color = new Color(0.2f, 0.16f, 0.1f, 1f);

            inputField.textComponent = text;
            inputField.textViewport = text.rectTransform;
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.asteriskChar = '*';
            inputField.keyboardType = TouchScreenKeyboardType.DecimalPad;
            inputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;

            RectTransform rect = valueTransform.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 48f);
            rect.anchoredPosition = new Vector2(300f, -10f);

            return inputField;
        }

        private static void FixMainMenuControllerReferences(Transform canvas)
        {
            GameObject mainMenuRoot = canvas.Find("MainMenu")?.gameObject;

            if (mainMenuRoot == null || !mainMenuRoot.TryGetComponent(out MainMenuController controller))
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("mainPanelRoot").objectReferenceValue = null;
            serialized.FindProperty("settingsPanelRoot").objectReferenceValue =
                canvas.Find("SettingsPanel")?.gameObject;
            serialized.FindProperty("settingsButtonOpenPanel").objectReferenceValue =
                canvas.Find("MainMenu/SettingsButton")?.GetComponent<Button>();
            serialized.FindProperty("settingsBackButton").objectReferenceValue =
                canvas.Find("SettingsPanel/BackButton")?.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetRect(Transform target, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void WireSettingsPanelController(GameObject settingsPanel)
        {
            WireSettingsPanelController(settingsPanel, null);
        }

        private static void RepositionMainMenuButtons(Transform mainMenu)
        {
            Transform host = mainMenu.Find("HostButton");
            Transform guest = mainMenu.Find("GuestButton");
            Transform settings = mainMenu.Find("SettingsButton");
            Transform quit = mainMenu.Find("QuitButton");

            if (host == null || guest == null || settings == null || quit == null)
            {
                return;
            }

            SetAnchoredY(host, 80f);
            SetAnchoredY(guest, 0f);
            SetAnchoredY(settings, -80f);
            SetAnchoredY(quit, -160f);
        }

        private static void SetAnchoredY(Transform target, float y)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            Vector2 pos = rect.anchoredPosition;
            pos.y = y;
            rect.anchoredPosition = pos;
        }

        private static void CreateLabel(Transform parent, string name, string text, Vector2 position, float width = 520f)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 48f);
            TMP_Text tmp = labelObject.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 30f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = text;
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
            background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.3f, 0.7f, 1f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 24f);
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
        }

        private static void CreateLanguageButton(Transform parent, string name, string text, Vector2 position)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200f, 56f);
            buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            Stretch(labelObject.GetComponent<RectTransform>());
            TMP_Text tmp = labelObject.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.text = text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
