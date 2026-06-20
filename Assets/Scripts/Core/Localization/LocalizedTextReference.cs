using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Catsss.Core.Localization
{
    /// <summary>
    /// Ссылка на строку Unity Localization + fallback для редактора и пустых таблиц.
    /// Переносимый блок: достаточно String Table и этого поля в инспекторе.
    /// </summary>
    [Serializable]
    public sealed class LocalizedTextReference
    {
        [SerializeField] private LocalizedString localizedText;
        [SerializeField] private string editorFallback = string.Empty;

        private Action<string> _changeHandler;
        private bool _waitingForLocalizationInit;

        public LocalizedString LocalizedText => localizedText;
        public string EditorFallback => editorFallback;

        public bool HasLocalization => localizedText != null && !localizedText.IsEmpty;

        public string ResolveDisplayText()
        {
            if (HasLocalization && TryGetLocalizedString(out string localized))
            {
                if (!string.IsNullOrWhiteSpace(localized))
                {
                    return localized;
                }
            }

            return editorFallback ?? string.Empty;
        }

        /// <summary>Подписка на смену локали / строки. Возвращает текущий текст сразу.</summary>
        public void Bind(Action<string> onTextChanged)
        {
            Unbind();

            if (onTextChanged == null)
            {
                return;
            }

            _changeHandler = onTextChanged;

            if (!HasLocalization)
            {
                onTextChanged.Invoke(editorFallback ?? string.Empty);
                return;
            }

            if (IsLocalizationReady())
            {
                ApplyBinding(onTextChanged);
                return;
            }

            // Localization ещё инициализируется (часто при первом Play / reload сцены).
            onTextChanged.Invoke(editorFallback ?? string.Empty);
            WaitForLocalizationInit();
        }

        public void Unbind()
        {
            StopWaitingForLocalizationInit();

            if (HasLocalization)
            {
                localizedText.StringChanged -= OnLocalizedStringChanged;
            }

            _changeHandler = null;
        }

        private void ApplyBinding(Action<string> onTextChanged)
        {
            onTextChanged.Invoke(ResolveDisplayText());
            localizedText.StringChanged += OnLocalizedStringChanged;
        }

        private void WaitForLocalizationInit()
        {
            if (_waitingForLocalizationInit || LocalizationSettings.Instance == null)
            {
                return;
            }

            var initOperation = LocalizationSettings.InitializationOperation;
            if (!initOperation.IsValid() || initOperation.IsDone)
            {
                if (_changeHandler != null && HasLocalization)
                {
                    ApplyBinding(_changeHandler);
                }

                return;
            }

            _waitingForLocalizationInit = true;
            initOperation.Completed += OnLocalizationInitCompleted;
        }

        private void StopWaitingForLocalizationInit()
        {
            if (!_waitingForLocalizationInit || LocalizationSettings.Instance == null)
            {
                _waitingForLocalizationInit = false;
                return;
            }

            var initOperation = LocalizationSettings.InitializationOperation;
            if (initOperation.IsValid())
            {
                initOperation.Completed -= OnLocalizationInitCompleted;
            }

            _waitingForLocalizationInit = false;
        }

        private void OnLocalizationInitCompleted(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<LocalizationSettings> _)
        {
            StopWaitingForLocalizationInit();

            if (_changeHandler == null || !HasLocalization)
            {
                return;
            }

            ApplyBinding(_changeHandler);
        }

        private bool TryGetLocalizedString(out string localized)
        {
            localized = null;

            if (!IsLocalizationReady())
            {
                return false;
            }

            try
            {
                localized = localizedText.GetLocalizedString();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[LocalizedTextReference] GetLocalizedString failed, using fallback. {exception.Message}");
                return false;
            }
        }

        private static bool IsLocalizationReady()
        {
            if (LocalizationSettings.Instance == null)
            {
                return false;
            }

            var initOperation = LocalizationSettings.InitializationOperation;
            return initOperation.IsValid() && initOperation.IsDone;
        }

        private void OnLocalizedStringChanged(string value)
        {
            if (_changeHandler == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                _changeHandler.Invoke(value);
                return;
            }

            _changeHandler.Invoke(editorFallback ?? string.Empty);
        }
    }
}
