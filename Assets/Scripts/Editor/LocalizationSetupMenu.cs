#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Catsss.EditorTools
{
    /// <summary>
    /// Одноразовая настройка RU+EN String Table для UI и world-подсказок.
    /// </summary>
    public static class LocalizationSetupMenu
    {
        private const string MenuPath = "Catsss/Localization/Setup UI Strings (RU + EN)";
        internal const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static readonly (string Key, string English, string Russian)[] DefaultEntries =
        {
            ("hint.interact.press_e", "Press E", "Нажмите E"),
            ("hint.trial.press_shift_dash", "Press Shift to dash", "Нажмите Shift для рывка"),
            ("menu.title", "Catsss", "Catsss"),
            ("menu.button.start", "Start game", "Начать игру"),
            ("menu.button.connect", "Connect", "Подключиться"),
            ("menu.button.back", "Back", "Назад"),
            ("menu.button.quit", "Quit", "Выйти"),
            ("menu.input.ip_placeholder", "Enter IP", "Введите IP"),
            ("menu.input.port_placeholder", "Enter Port", "Введите порт"),
            ("menu.guest.ip_hint", "Host IP for guest (LAN / Hamachi): {0}", "IP этой машины для гостя (LAN / Hamachi): {0}"),
            ("menu.network.no_ip", "Not found", "не найден"),
            ("menu.error.connection_failed", "Could not connect to the host. Check IP, port, and that the host is running.", "Не удалось подключиться к хосту. Проверьте IP, порт и что хост запущен."),
            ("menu.error.empty_host", "Enter the host IP or hostname.", "Введите IP или имя хоста."),
            ("menu.error.invalid_address", "Invalid IP or hostname. Example: 192.168.0.5 or localhost", "Неверный IP или имя хоста. Пример: 192.168.0.5 или localhost"),
            ("menu.error.invalid_port", "Invalid port. Enter a number from 1 to 65535.", "Неверный порт. Введите число от 1 до 65535."),
        };

        [MenuItem(MenuPath)]
        public static void SetupUiStrings()
        {
            EnsureLocalizationSettings();
            Locale english = GetOrCreateLocale("en", "English", "en");
            Locale russian = GetOrCreateLocale("ru", "Russian (ru)", "ru");

            StringTableCollection collection = GetOrCreateCollection("UI_Strings", "Assets/Localization/StringTables");

            foreach ((string key, string englishText, string russianText) in DefaultEntries)
            {
                UpsertEntry(collection, key, english, englishText);
                UpsertEntry(collection, key, russian, russianText);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Catsss] Localization setup complete: UI_Strings (en + ru).");
        }

        private static void EnsureLocalizationSettings()
        {
            if (LocalizationSettings.Instance != null)
            {
                return;
            }

            const string settingsPath = "Assets/Localization/Localization Settings.asset";
            if (!System.IO.File.Exists(settingsPath))
            {
                System.IO.Directory.CreateDirectory("Assets/Localization");
                var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
        }

        private static Locale GetOrCreateLocale(string code, string displayName, string localeCode)
        {
            Locale[] locales = LocalizationEditorSettings.GetLocales().ToArray();

            foreach (Locale locale in locales)
            {
                if (locale.Identifier.Code == code)
                {
                    return locale;
                }
            }

            System.IO.Directory.CreateDirectory("Assets/Localization/Locales");
            string path = $"Assets/Localization/Locales/{displayName.Replace(' ', '_')}.asset";
            var localeAsset = Locale.CreateLocale(new LocaleIdentifier(code));
            AssetDatabase.CreateAsset(localeAsset, path);
            LocalizationEditorSettings.AddLocale(localeAsset);
            return localeAsset;
        }

        private static StringTableCollection GetOrCreateCollection(string tableName, string folder)
        {
            System.IO.Directory.CreateDirectory(folder);

            foreach (StringTableCollection existing in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (existing.name == tableName)
                {
                    return existing;
                }
            }

            return LocalizationEditorSettings.CreateStringTableCollection(tableName, folder);
        }

        private static void UpsertEntry(StringTableCollection collection, string key, Locale locale, string value)
        {
            StringTable table = collection.GetTable(locale.Identifier) as StringTable;

            if (table == null)
            {
                Debug.LogWarning($"[Catsss] String table for locale '{locale.Identifier.Code}' not found.");
                return;
            }

            StringTableEntry entry = table.GetEntry(key) ?? table.AddEntry(key, value);

            if (entry != null)
            {
                entry.Value = value;
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
            }
        }
    }
}
#endif
