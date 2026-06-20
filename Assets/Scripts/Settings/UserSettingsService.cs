using System;
using Catsss.Core.Services;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Catsss.Settings
{
    /// <summary>
    /// Пользовательские настройки (чувствительность мыши, язык). Живёт в ServiceLocator, сохраняется в PlayerPrefs.
    /// </summary>
    public sealed class UserSettingsService
    {
        public const float MinMouseSensitivityMultiplier = 0.25f;
        public const float MaxMouseSensitivityMultiplier = 2f;
        public const float DefaultMouseSensitivityMultiplier = 1f;

        private const string MouseSensitivityPrefKey = "catsss.settings.mouse_sensitivity";
        private const string LocalePrefKey = "catsss.settings.locale";

        public float MouseSensitivityMultiplier { get; private set; } = DefaultMouseSensitivityMultiplier;

        public event Action SettingsChanged;

        public void Load()
        {
            MouseSensitivityMultiplier = Mathf.Clamp(
                PlayerPrefs.GetFloat(MouseSensitivityPrefKey, DefaultMouseSensitivityMultiplier),
                MinMouseSensitivityMultiplier,
                MaxMouseSensitivityMultiplier);

            ApplySavedLocale();
        }

        public void SetMouseSensitivityMultiplier(float multiplier)
        {
            float clamped = Mathf.Clamp(multiplier, MinMouseSensitivityMultiplier, MaxMouseSensitivityMultiplier);

            if (Mathf.Approximately(MouseSensitivityMultiplier, clamped))
            {
                return;
            }

            MouseSensitivityMultiplier = clamped;
            PlayerPrefs.SetFloat(MouseSensitivityPrefKey, clamped);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public void SetLanguage(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode));

            if (locale == null)
            {
                Debug.LogWarning($"[UserSettingsService] Locale '{localeCode}' not found.");
                return;
            }

            if (LocalizationSettings.SelectedLocale == locale)
            {
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(LocalePrefKey, localeCode);
            PlayerPrefs.Save();
            SettingsChanged?.Invoke();
        }

        public string GetCurrentLocaleCode()
        {
            Locale selected = LocalizationSettings.SelectedLocale;
            return selected != null ? selected.Identifier.Code : "en";
        }

        public static UserSettingsService GetOrThrow()
        {
            return ServiceLocator.Get<UserSettingsService>();
        }

        private void ApplySavedLocale()
        {
            string code = PlayerPrefs.GetString(LocalePrefKey, "en");
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));

            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }
    }
}
