#if UNITY_EDITOR
using Catsss.Core.Localization;
using Catsss.Menu;
using Catsss.Menu.Levels;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Catsss.EditorTools
{
    public static class MainMenuLocalizationSetup
    {
        private const string MenuPath = "Catsss/Localization/Setup MainMenu Scene Texts";

        private static readonly (string RelativePath, string Key, string EnglishFallback)[] TextBindings =
        {
            ("Canvas/MainMenu/Title", "menu.title", "Catsss"),
            ("Canvas/MainMenu/HostButton/Text (TMP)", "menu.button.start", "Start game"),
            ("Canvas/MainMenu/GuestButton/Text (TMP)", "menu.button.connect", "Connect"),
            ("Canvas/MainMenu/SettingsButton/Text (TMP)", "menu.button.settings", "Settings"),
            ("Canvas/MainMenu/QuitButton/Text (TMP)", "menu.button.quit", "Quit"),
            ("Canvas/GuestPanel/Title", "menu.title", "Catsss"),
            ("Canvas/GuestPanel/BackButton/Text (TMP)", "menu.button.back", "Back"),
            ("Canvas/GuestPanel/ConnectButton/Text (TMP)", "menu.button.connect", "Connect"),
            ("Canvas/GuestPanel/InputFieldIP/Placeholder", "menu.input.ip_placeholder", "Enter IP"),
            ("Canvas/GuestPanel/InputFieldPort/Placeholder", "menu.input.port_placeholder", "Enter Port"),
            ("Canvas/SettingsPanel/Title", "settings.title", "Settings"),
            ("Canvas/SettingsPanel/BackButton/Text (TMP)", "menu.button.back", "Back"),
            ("Canvas/SettingsPanel/SensitivityLabel", "settings.sensitivity", "Mouse sensitivity"),
            ("Canvas/SettingsPanel/LanguageLabel", "settings.language", "Language"),
            ("Canvas/SettingsPanel/EnglishButton/Text (TMP)", "settings.language.english", "English"),
            ("Canvas/SettingsPanel/RussianButton/Text (TMP)", "settings.language.russian", "Russian"),
            ("Canvas/LevelSelectPanel/Title", "menu.level_select.title", "Levels"),
            ("Canvas/LevelSelectPanel/BackButton/Text (TMP)", "menu.button.back", "Back"),
            ("Canvas/LevelSelectPanel/PlayButton/Text (TMP)", "menu.level_select.play", "Play"),
        };

        [MenuItem(MenuPath)]
        public static void SetupMainMenuSceneTexts()
        {
            LocalizationSetupMenu.SetupUiStrings();

            Scene scene = EditorSceneManager.OpenScene(LocalizationSetupMenu.MainMenuScenePath, OpenSceneMode.Single);
            Transform canvasRoot = FindRequiredTransform("Canvas");
            StringTableCollection collection = GetUiStringsCollection();

            int wired = 0;

            foreach ((string relativePath, string key, string englishFallback) in TextBindings)
            {
                Transform target = FindRelative(canvasRoot, relativePath);

                if (target == null)
                {
                    Debug.LogWarning($"[Catsss] MainMenu: не найден объект '{relativePath}'.");
                    continue;
                }

                LocalizedUiText localizedUiText = GetOrAddComponent<LocalizedUiText>(target.gameObject);
                AssignLocalizedReference(localizedUiText, collection, key, englishFallback);
                wired++;
            }

            GameObject mainMenuRoot = GameObject.Find("MainMenu");

            if (mainMenuRoot == null)
            {
                Debug.LogError("[Catsss] MainMenu: объект MainMenu не найден на сцене.");
                return;
            }

            MainMenuController controller = mainMenuRoot.GetComponent<MainMenuController>();

            if (controller == null)
            {
                Debug.LogError("[Catsss] MainMenu: на объекте MainMenu нет MainMenuController.");
                return;
            }

            MainMenuLocalizedText menuLocalizedText = GetOrAddComponent<MainMenuLocalizedText>(mainMenuRoot);
            AssignLocalizedReference(menuLocalizedText, "ipv4HintFormat", collection, "menu.guest.ip_hint", "Host IP for guest (LAN / Hamachi): {0}");
            AssignLocalizedReference(menuLocalizedText, "noIpFoundText", collection, "menu.network.no_ip", "Not found");
            AssignLocalizedReference(menuLocalizedText, "connectionFailedText", collection, "menu.error.connection_failed",
                "Could not connect to the host. Check IP, port, and that the host is running.");
            AssignLocalizedReference(menuLocalizedText, "emptyHostText", collection, "menu.error.empty_host", "Enter the host IP or hostname.");
            AssignLocalizedReference(menuLocalizedText, "invalidAddressText", collection, "menu.error.invalid_address",
                "Invalid IP or hostname. Example: 192.168.0.5 or localhost");
            AssignLocalizedReference(menuLocalizedText, "invalidPortText", collection, "menu.error.invalid_port",
                "Invalid port. Enter a number from 1 to 65535.");
            AssignLocalizedReference(menuLocalizedText, "hostDisconnectedText", collection, "menu.banner.host_disconnected",
                "Host disconnected.");
            AssignLocalizedReference(menuLocalizedText, "sessionEndedText", collection, "menu.banner.session_ended",
                "Session ended.");
            AssignLocalizedReference(menuLocalizedText, "sceneLoadFailedText", collection, "menu.error.scene_load_failed",
                "Could not load level \"{0}\". Add it to Build Profiles and check the scene name in Level Catalog.");

            LevelSelectController levelSelect = FindRelative(canvasRoot, "Canvas/LevelSelectPanel")?.GetComponent<LevelSelectController>();

            if (levelSelect != null)
            {
                AssignLocalizedReference(levelSelect, "lockedHintText", collection, "menu.level_select.locked", "Locked");
            }

            Transform mainMenuPanel = FindRelative(canvasRoot, "Canvas/MainMenu");
            if (mainMenuPanel != null)
            {
                TMP_Text sessionBannerTmp = EnsureSessionBannerText(mainMenuPanel);
                SerializedObject menuTextSerialized = new SerializedObject(menuLocalizedText);
                menuTextSerialized.FindProperty("sessionBannerTmp").objectReferenceValue = sessionBannerTmp;
                menuTextSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menuLocalizedText);
            }

            Transform guestPanel = FindRelative(canvasRoot, "Canvas/GuestPanel");
            if (guestPanel != null)
            {
                TMP_Text connectionErrorTmp = EnsureConnectionErrorText(guestPanel);
                SerializedObject menuTextSerialized = new SerializedObject(menuLocalizedText);
                menuTextSerialized.FindProperty("connectionErrorTmp").objectReferenceValue = connectionErrorTmp;
                menuTextSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menuLocalizedText);
            }

            SerializedObject controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("localizedText").objectReferenceValue = menuLocalizedText;
            controllerSerialized.FindProperty("mainPanelRoot").objectReferenceValue = null;
            controllerSerialized.FindProperty("settingsPanelRoot").objectReferenceValue =
                FindRelative(canvasRoot, "Canvas/SettingsPanel")?.gameObject;
            controllerSerialized.FindProperty("settingsButtonOpenPanel").objectReferenceValue =
                FindRelative(canvasRoot, "Canvas/MainMenu/SettingsButton")?.GetComponent<Button>();
            controllerSerialized.FindProperty("settingsBackButton").objectReferenceValue =
                FindRelative(canvasRoot, "Canvas/SettingsPanel/BackButton")?.GetComponent<Button>();
            controllerSerialized.FindProperty("levelSelectPanelRoot").objectReferenceValue =
                FindRelative(canvasRoot, "Canvas/LevelSelectPanel")?.gameObject;
            controllerSerialized.FindProperty("levelSelectController").objectReferenceValue = levelSelect;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Catsss] MainMenu localization wired: {wired} static texts + IP hint + connection error.");
        }

        private static TMP_Text EnsureConnectionErrorText(Transform guestPanel)
        {
            Transform existing = guestPanel.Find("ConnectionErrorText");

            if (existing != null && existing.TryGetComponent(out TMP_Text existingTmp))
            {
                existing.gameObject.SetActive(false);
                return existingTmp;
            }

            var errorObject = new GameObject("ConnectionErrorText", typeof(RectTransform), typeof(TextMeshProUGUI));
            errorObject.transform.SetParent(guestPanel, false);
            errorObject.SetActive(false);

            RectTransform rectTransform = errorObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -210f);
            rectTransform.sizeDelta = new Vector2(760f, 90f);

            TMP_Text tmp = errorObject.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 28f;
            tmp.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.text = "Connection error";

            return tmp;
        }

        private static TMP_Text EnsureSessionBannerText(Transform mainMenuPanel)
        {
            Transform existing = mainMenuPanel.Find("SessionBannerText");

            if (existing != null && existing.TryGetComponent(out TMP_Text existingTmp))
            {
                existing.gameObject.SetActive(false);
                return existingTmp;
            }

            var bannerObject = new GameObject("SessionBannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bannerObject.transform.SetParent(mainMenuPanel, false);
            bannerObject.SetActive(false);

            RectTransform rectTransform = bannerObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 24f);
            rectTransform.sizeDelta = new Vector2(760f, 72f);

            TMP_Text tmp = bannerObject.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 26f;
            tmp.color = new Color(0.95f, 0.75f, 0.2f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.text = "Session ended";

            return tmp;
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

            Debug.LogError("[Catsss] UI_Strings collection not found. Run Setup UI Strings first.");
            return null;
        }

        private static Transform FindRequiredTransform(string name)
        {
            GameObject root = GameObject.Find(name);
            return root != null ? root.transform : null;
        }

        private static Transform FindRelative(Transform root, string relativePath)
        {
            if (root == null)
            {
                return null;
            }

            string[] parts = relativePath.Split('/');
            Transform current = root;

            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);

                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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

            SerializedObject serialized = new SerializedObject(component);
            ApplyLocalizedString(serialized.FindProperty("textReference"), collection, key, englishFallback);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static void AssignLocalizedReference(
            MainMenuLocalizedText component,
            string propertyName,
            StringTableCollection collection,
            string key,
            string englishFallback)
        {
            if (collection == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(component);
            ApplyLocalizedString(serialized.FindProperty(propertyName), collection, key, englishFallback);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static void AssignLocalizedReference(
            LevelSelectController component,
            string propertyName,
            StringTableCollection collection,
            string key,
            string englishFallback)
        {
            if (collection == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(component);
            ApplyLocalizedString(serialized.FindProperty(propertyName), collection, key, englishFallback);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static void ApplyLocalizedString(
            SerializedProperty rootProperty,
            StringTableCollection collection,
            string key,
            string englishFallback)
        {
            if (rootProperty == null)
            {
                return;
            }

            SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(key);

            if (sharedEntry == null)
            {
                Debug.LogWarning($"[Catsss] UI_Strings has no key '{key}'.");
                return;
            }

            SerializedProperty localizedText = rootProperty.FindPropertyRelative("localizedText");
            SerializedProperty tableReference = localizedText.FindPropertyRelative("m_TableReference");
            tableReference.FindPropertyRelative("m_TableCollectionName").stringValue = collection.TableCollectionNameReference.TableCollectionName;

            SerializedProperty entryReference = localizedText.FindPropertyRelative("m_TableEntryReference");
            entryReference.FindPropertyRelative("m_KeyId").longValue = sharedEntry.Id;
            entryReference.FindPropertyRelative("m_Key").stringValue = key;

            rootProperty.FindPropertyRelative("editorFallback").stringValue = englishFallback;
        }
    }
}
#endif
