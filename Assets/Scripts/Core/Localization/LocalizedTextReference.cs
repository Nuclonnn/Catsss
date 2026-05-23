using System;
using UnityEngine;
using UnityEngine.Localization;

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

        public LocalizedString LocalizedText => localizedText;
        public string EditorFallback => editorFallback;

        public bool HasLocalization => localizedText != null && !localizedText.IsEmpty;

        public string ResolveDisplayText()
        {
            if (HasLocalization)
            {
                string localized = localizedText.GetLocalizedString();

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
            onTextChanged.Invoke(ResolveDisplayText());

            if (HasLocalization)
            {
                localizedText.StringChanged += OnLocalizedStringChanged;
            }
        }

        public void Unbind()
        {
            if (HasLocalization)
            {
                localizedText.StringChanged -= OnLocalizedStringChanged;
            }

            _changeHandler = null;
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
