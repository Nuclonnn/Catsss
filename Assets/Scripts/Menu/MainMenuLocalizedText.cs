using Catsss.Core.Localization;
using Catsss.Network;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Catsss.Menu
{
    /// <summary>
    /// Динамические строки меню (IP-хинт, ошибки guest panel), обновляются при смене локали.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuLocalizedText : MonoBehaviour
    {
        [Header("IPv4 hint (optional)")]
        [SerializeField] private TMP_Text ipv4HintTmp;
        [SerializeField] private Text ipv4HintLegacy;
        [SerializeField] private LocalizedTextReference ipv4HintFormat = new();
        [SerializeField] private LocalizedTextReference noIpFoundText = new();

        [Header("Guest panel errors")]
        [SerializeField] private TMP_Text connectionErrorTmp;
        [SerializeField] private Text connectionErrorLegacy;
        [SerializeField] private LocalizedTextReference connectionFailedText = new();
        [SerializeField] private LocalizedTextReference emptyHostText = new();
        [SerializeField] private LocalizedTextReference invalidAddressText = new();
        [SerializeField] private LocalizedTextReference invalidPortText = new();

        private LocalizedTextReference _activeErrorText;
        private bool _guestErrorVisible;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshIpv4Hint();

            if (_guestErrorVisible)
            {
                BindActiveGuestError();
            }
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            ipv4HintFormat.Unbind();
            noIpFoundText.Unbind();
            UnbindAllGuestErrors();
        }

        private void Start()
        {
            RefreshIpv4Hint();
        }

        public void RefreshIpv4Hint()
        {
            if (ipv4HintTmp == null && ipv4HintLegacy == null)
            {
                return;
            }

            string addresses = NetworkLocalAddressHints.BuildLanIpv4Csv();

            if (string.IsNullOrEmpty(addresses))
            {
                addresses = noIpFoundText.ResolveDisplayText();
            }

            string format = ipv4HintFormat.ResolveDisplayText();
            ApplyIpv4Hint(string.Format(format, addresses));
        }

        public void ShowConnectionError()
        {
            ShowGuestError(connectionFailedText);
        }

        public void ShowConnectInputError(ClientConnectInputError error)
        {
            LocalizedTextReference text = error switch
            {
                ClientConnectInputError.EmptyHost => emptyHostText,
                ClientConnectInputError.InvalidPort => invalidPortText,
                ClientConnectInputError.InvalidHost => invalidAddressText,
                _ => invalidAddressText,
            };

            ShowGuestError(text);
        }

        public void ClearGuestError()
        {
            _guestErrorVisible = false;
            UnbindAllGuestErrors();
            SetGuestErrorVisible(false);
        }

        private void ShowGuestError(LocalizedTextReference text)
        {
            if (connectionErrorTmp == null && connectionErrorLegacy == null)
            {
                Debug.LogWarning("[MainMenuLocalizedText] Нет UI для ошибки (ConnectionErrorText).");
                return;
            }

            _guestErrorVisible = true;
            _activeErrorText = text;
            BindActiveGuestError();
            SetGuestErrorVisible(true);
        }

        private void BindActiveGuestError()
        {
            LocalizedTextReference target = _activeErrorText;
            UnbindAllGuestErrors();
            _activeErrorText = target;

            if (_activeErrorText != null)
            {
                _activeErrorText.Bind(ApplyGuestError);
            }
        }

        private void UnbindAllGuestErrors()
        {
            _activeErrorText?.Unbind();
            _activeErrorText = null;
            connectionFailedText.Unbind();
            emptyHostText.Unbind();
            invalidAddressText.Unbind();
            invalidPortText.Unbind();
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            RefreshIpv4Hint();

            if (_guestErrorVisible && _activeErrorText != null)
            {
                ApplyGuestError(_activeErrorText.ResolveDisplayText());
            }
        }

        private void ApplyIpv4Hint(string value)
        {
            if (ipv4HintTmp != null)
            {
                ipv4HintTmp.text = value;
            }

            if (ipv4HintLegacy != null)
            {
                ipv4HintLegacy.text = value;
            }
        }

        private void ApplyGuestError(string value)
        {
            if (connectionErrorTmp != null)
            {
                connectionErrorTmp.text = value ?? string.Empty;
            }

            if (connectionErrorLegacy != null)
            {
                connectionErrorLegacy.text = value ?? string.Empty;
            }
        }

        private void SetGuestErrorVisible(bool visible)
        {
            if (connectionErrorTmp != null)
            {
                connectionErrorTmp.gameObject.SetActive(visible);
            }

            if (connectionErrorLegacy != null)
            {
                connectionErrorLegacy.gameObject.SetActive(visible);
            }
        }
    }
}
