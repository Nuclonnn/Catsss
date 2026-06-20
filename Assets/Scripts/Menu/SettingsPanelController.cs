using System.Globalization;
using Catsss.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Catsss.Menu
{
    /// <summary>
    /// Панель настроек: чувствительность мыши и язык. Переиспользуется в MainMenu и (позже) в паузе.
    /// </summary>
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField]
        private Slider mouseSensitivitySlider;

        [SerializeField]
        private TMP_InputField sensitivityInputField;

        [SerializeField]
        private TMP_Text sensitivityValueTmp;

        [SerializeField]
        private Text sensitivityValueLegacy;

        [Header("Language")]
        [SerializeField]
        private Button englishButton;

        [SerializeField]
        private Button russianButton;

        [SerializeField]
        private Color selectedLanguageTint = new(0.92f, 0.86f, 0.72f, 1f);

        [SerializeField]
        private Color normalLanguageTint = Color.white;

        private UserSettingsService _settings;
        private bool _suppressSensitivitySync;

        private void OnEnable()
        {
            _settings = UserSettingsService.GetOrThrow();
            BindUi();
            RefreshFromSettings();
        }

        private void OnDisable()
        {
            UnbindUi();
        }

        public void RefreshFromSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _suppressSensitivitySync = true;

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = UserSettingsService.MinMouseSensitivityMultiplier;
                mouseSensitivitySlider.maxValue = UserSettingsService.MaxMouseSensitivityMultiplier;
                mouseSensitivitySlider.value = _settings.MouseSensitivityMultiplier;
            }

            _suppressSensitivitySync = false;
            ApplySensitivityDisplay(_settings.MouseSensitivityMultiplier);
            RefreshLanguageHighlights(_settings.GetCurrentLocaleCode());
        }

        private void BindUi()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
            }

            if (sensitivityInputField != null)
            {
                sensitivityInputField.onEndEdit.AddListener(OnSensitivityInputEndEdit);
            }

            englishButton?.onClick.AddListener(OnEnglishClicked);
            russianButton?.onClick.AddListener(OnRussianClicked);
        }

        private void UnbindUi()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySliderChanged);
            }

            if (sensitivityInputField != null)
            {
                sensitivityInputField.onEndEdit.RemoveListener(OnSensitivityInputEndEdit);
            }

            englishButton?.onClick.RemoveListener(OnEnglishClicked);
            russianButton?.onClick.RemoveListener(OnRussianClicked);
        }

        private void OnSensitivitySliderChanged(float value)
        {
            if (_suppressSensitivitySync || _settings == null)
            {
                return;
            }

            _settings.SetMouseSensitivityMultiplier(value);
            ApplySensitivityDisplay(_settings.MouseSensitivityMultiplier);
        }

        private void OnSensitivityInputEndEdit(string rawText)
        {
            if (_suppressSensitivitySync || _settings == null)
            {
                return;
            }

            if (!TryParseSensitivity(rawText, out float parsed))
            {
                ApplySensitivityDisplay(_settings.MouseSensitivityMultiplier);
                return;
            }

            _settings.SetMouseSensitivityMultiplier(parsed);
            SyncSliderWithoutCallback(_settings.MouseSensitivityMultiplier);
            ApplySensitivityDisplay(_settings.MouseSensitivityMultiplier);
        }

        private void SyncSliderWithoutCallback(float value)
        {
            if (mouseSensitivitySlider == null)
            {
                return;
            }

            _suppressSensitivitySync = true;
            mouseSensitivitySlider.value = value;
            _suppressSensitivitySync = false;
        }

        private void OnEnglishClicked()
        {
            _settings?.SetLanguage("en");
            RefreshLanguageHighlights("en");
        }

        private void OnRussianClicked()
        {
            _settings?.SetLanguage("ru");
            RefreshLanguageHighlights("ru");
        }

        private void ApplySensitivityDisplay(float multiplier)
        {
            string text = multiplier.ToString("0.00", CultureInfo.InvariantCulture);

            if (sensitivityInputField != null)
            {
                sensitivityInputField.SetTextWithoutNotify(text);
            }

            if (sensitivityValueTmp != null)
            {
                sensitivityValueTmp.text = text;
            }

            if (sensitivityValueLegacy != null)
            {
                sensitivityValueLegacy.text = text;
            }
        }

        private void RefreshLanguageHighlights(string localeCode)
        {
            bool isEnglish = localeCode == "en";
            ApplyLanguageButtonTint(englishButton, isEnglish);
            ApplyLanguageButtonTint(russianButton, !isEnglish);
        }

        private void ApplyLanguageButtonTint(Button button, bool selected)
        {
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return;
            }

            image.color = selected ? selectedLanguageTint : normalLanguageTint;
        }

        public static bool TryParseSensitivity(string rawText, out float value)
        {
            value = UserSettingsService.DefaultMouseSensitivityMultiplier;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            string normalized = rawText.Trim().Replace(',', '.');

            if (!float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float parsed))
            {
                return false;
            }

            value = Mathf.Clamp(
                parsed,
                UserSettingsService.MinMouseSensitivityMultiplier,
                UserSettingsService.MaxMouseSensitivityMultiplier);
            return true;
        }
    }
}
