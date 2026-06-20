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

        [Header("Session return banner")]
        [SerializeField] private TMP_Text sessionBannerTmp;
        [SerializeField] private Text sessionBannerLegacy;
        [SerializeField] private LocalizedTextReference hostDisconnectedText = new();
        [SerializeField] private LocalizedTextReference sessionEndedText = new();
        [SerializeField] private LocalizedTextReference sceneLoadFailedText = new();

        private LocalizedTextReference _activeErrorText;
        private bool _guestErrorVisible;
        private LocalizedTextReference _activeSessionBannerText;
        private bool _sessionBannerVisible;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshIpv4Hint();

            if (_guestErrorVisible)
            {
                BindActiveGuestError();
            }

            if (_sessionBannerVisible)
            {
                BindActiveSessionBanner();
            }
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            ipv4HintFormat.Unbind();
            noIpFoundText.Unbind();
            UnbindAllGuestErrors();
            UnbindSessionBanner();
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

        public void ShowSessionBanner(MenuReturnReason reason, string context = null)
        {
            if (reason == MenuReturnReason.SceneLoadFailed)
            {
                string format = sceneLoadFailedText.ResolveDisplayText();
                string sceneName = string.IsNullOrWhiteSpace(context) ? "?" : context;
                ApplySessionBanner(string.Format(format, sceneName));
                SetSessionBannerVisible(true);
                return;
            }

            LocalizedTextReference text = reason switch
            {
                MenuReturnReason.HostDisconnected => hostDisconnectedText,
                MenuReturnReason.SessionEnded => sessionEndedText,
                MenuReturnReason.SceneLoadFailed => sceneLoadFailedText,
                _ => sessionEndedText,
            };

            if (sessionBannerTmp == null && sessionBannerLegacy == null)
            {
                Debug.LogWarning("[MainMenuLocalizedText] Нет UI для session banner.");
                return;
            }

            _sessionBannerVisible = true;
            _activeSessionBannerText = text;
            BindActiveSessionBanner();
            SetSessionBannerVisible(true);
        }

        public void ClearSessionBanner()
        {
            _sessionBannerVisible = false;
            UnbindSessionBanner();
            SetSessionBannerVisible(false);
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

            if (_sessionBannerVisible && _activeSessionBannerText != null)
            {
                ApplySessionBanner(_activeSessionBannerText.ResolveDisplayText());
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

        private void BindActiveSessionBanner()
        {
            LocalizedTextReference target = _activeSessionBannerText;
            UnbindSessionBanner();
            _activeSessionBannerText = target;

            if (_activeSessionBannerText != null)
            {
                _activeSessionBannerText.Bind(ApplySessionBanner);
            }
        }

        private void UnbindSessionBanner()
        {
            _activeSessionBannerText?.Unbind();
            _activeSessionBannerText = null;
            hostDisconnectedText.Unbind();
            sessionEndedText.Unbind();
            sceneLoadFailedText.Unbind();
        }

        private void ApplySessionBanner(string value)
        {
            if (sessionBannerTmp != null)
            {
                sessionBannerTmp.text = value ?? string.Empty;
            }

            if (sessionBannerLegacy != null)
            {
                sessionBannerLegacy.text = value ?? string.Empty;
            }
        }

        private void SetSessionBannerVisible(bool visible)
        {
            if (sessionBannerTmp != null)
            {
                sessionBannerTmp.gameObject.SetActive(visible);
            }

            if (sessionBannerLegacy != null)
            {
                sessionBannerLegacy.gameObject.SetActive(visible);
            }
        }
    }
}
