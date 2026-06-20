using System;
using System.Collections.Generic;
using Catsss.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Catsss.Menu.Levels
{
    /// <summary>
    /// Карусель выбора уровня: стрелки, точки, карточка с превью. Хост запускает только разблокированный уровень.
    /// </summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        private const string UiStringsTable = "UI_Strings";

        [SerializeField]
        private LevelCatalog levelCatalog;

        [Header("Card")]
        [SerializeField]
        private Image thumbnailImage;

        [SerializeField]
        private TMP_Text levelNameTmp;

        [SerializeField]
        private GameObject lockOverlayRoot;

        [SerializeField]
        private CanvasGroup cardCanvasGroup = null;

        [SerializeField, Range(0.2f, 1f)]
        private float lockedCardAlpha = 0.45f;

        [Header("Navigation")]
        [SerializeField]
        private Button previousButton;

        [SerializeField]
        private Button nextButton;

        [SerializeField]
        private Transform dotsRoot;

        [SerializeField]
        private Image dotPrefab;

        [Header("Actions")]
        [SerializeField]
        private Button playButton;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private LocalizedTextReference lockedHintText = new();

        [SerializeField]
        private TMP_Text lockedHintTmp;

        private readonly List<Image> _dotInstances = new();
        private LocalizedString _levelNameLocalized;
        private int _selectedIndex;
        private bool _lockedHintVisible;

        public event Action<string> HostLevelRequested;

        public event Action BackRequested;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            previousButton?.onClick.AddListener(SelectPrevious);
            nextButton?.onClick.AddListener(SelectNext);
            playButton?.onClick.AddListener(OnPlayClicked);
            backButton?.onClick.AddListener(OnBackClicked);
            BindLockedHint();
            RebuildDots();
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, GetLevelCount() - 1));
            RefreshSelection();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            previousButton?.onClick.RemoveListener(SelectPrevious);
            nextButton?.onClick.RemoveListener(SelectNext);
            playButton?.onClick.RemoveListener(OnPlayClicked);
            backButton?.onClick.RemoveListener(OnBackClicked);
            UnbindLevelName();
            UnbindLockedHint();
        }

        public void ResetToFirstLevel()
        {
            _selectedIndex = 0;
            RefreshSelection();
        }

        public void NavigatePrevious() => SelectPrevious();

        public void NavigateNext() => SelectNext();

        private void SelectPrevious()
        {
            int count = GetLevelCount();

            if (count <= 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex - 1 + count) % count;
            RefreshSelection();
        }

        private void SelectNext()
        {
            int count = GetLevelCount();

            if (count <= 0)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + 1) % count;
            RefreshSelection();
        }

        private void OnPlayClicked()
        {
            if (!levelCatalog.TryGetLevel(_selectedIndex, out LevelDefinition level) || !level.IsUnlocked)
            {
                return;
            }

            HostLevelRequested?.Invoke(level.SceneName);
        }

        private void OnBackClicked()
        {
            BackRequested?.Invoke();
        }

        private void RefreshSelection()
        {
            int count = GetLevelCount();
            SetNavigationVisible(count > 1);
            UpdateDots();

            if (levelCatalog == null || count <= 0 ||
                !levelCatalog.TryGetLevel(_selectedIndex, out LevelDefinition level))
            {
                ApplyEmptyCard();
                return;
            }

            ApplyLevelCard(level);
        }

        private void ApplyLevelCard(LevelDefinition level)
        {
            bool locked = !level.IsUnlocked;

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = level.Thumbnail;
                thumbnailImage.enabled = level.Thumbnail != null;
                thumbnailImage.color = locked ? new Color(0.65f, 0.65f, 0.65f, 1f) : Color.white;
            }

            BindLevelName(level.DisplayNameKey);

            if (lockOverlayRoot != null)
            {
                lockOverlayRoot.SetActive(locked);
            }

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = locked ? lockedCardAlpha : 1f;
            }

            if (playButton != null)
            {
                playButton.interactable = !locked;
            }

            _lockedHintVisible = locked;
            SetLockedHintVisible(locked);

            if (locked)
            {
                BindLockedHint();
            }
            else
            {
                UnbindLockedHint();
            }
        }

        private void ApplyEmptyCard()
        {
            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = null;
                thumbnailImage.enabled = false;
            }

            if (levelNameTmp != null)
            {
                levelNameTmp.text = string.Empty;
            }

            if (lockOverlayRoot != null)
            {
                lockOverlayRoot.SetActive(false);
            }

            if (playButton != null)
            {
                playButton.interactable = false;
            }

            _lockedHintVisible = false;
            SetLockedHintVisible(false);
        }

        private void BindLevelName(string key)
        {
            UnbindLevelName();

            if (levelNameTmp == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _levelNameLocalized = new LocalizedString(UiStringsTable, key);
            _levelNameLocalized.StringChanged += OnLevelNameChanged;
            levelNameTmp.text = _levelNameLocalized.GetLocalizedString() ?? key;
        }

        private void UnbindLevelName()
        {
            if (_levelNameLocalized != null)
            {
                _levelNameLocalized.StringChanged -= OnLevelNameChanged;
                _levelNameLocalized = null;
            }
        }

        private void OnLevelNameChanged(string value)
        {
            if (levelNameTmp != null)
            {
                levelNameTmp.text = string.IsNullOrWhiteSpace(value)
                    ? _levelNameLocalized?.TableEntryReference.Key ?? string.Empty
                    : value;
            }
        }

        private void BindLockedHint()
        {
            if (!_lockedHintVisible)
            {
                return;
            }

            lockedHintText.Bind(ApplyLockedHint);
        }

        private void UnbindLockedHint()
        {
            lockedHintText.Unbind();
        }

        private void ApplyLockedHint(string value)
        {
            if (lockedHintTmp != null)
            {
                lockedHintTmp.text = value ?? string.Empty;
            }
        }

        private void SetLockedHintVisible(bool visible)
        {
            if (lockedHintTmp != null)
            {
                lockedHintTmp.gameObject.SetActive(visible);
            }
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            RefreshSelection();
        }

        private void RebuildDots()
        {
            ClearDots();

            if (dotsRoot == null || dotPrefab == null)
            {
                return;
            }

            int count = GetLevelCount();

            for (int i = 0; i < count; i++)
            {
                Image dot = Instantiate(dotPrefab, dotsRoot);
                dot.gameObject.SetActive(true);
                _dotInstances.Add(dot);
            }

            dotPrefab.gameObject.SetActive(false);
        }

        private void ClearDots()
        {
            for (int i = _dotInstances.Count - 1; i >= 0; i--)
            {
                if (_dotInstances[i] != null)
                {
                    Destroy(_dotInstances[i].gameObject);
                }
            }

            _dotInstances.Clear();
        }

        private void UpdateDots()
        {
            for (int i = 0; i < _dotInstances.Count; i++)
            {
                Image dot = _dotInstances[i];

                if (dot == null)
                {
                    continue;
                }

                dot.color = i == _selectedIndex
                    ? new Color(0.15f, 0.12f, 0.08f, 1f)
                    : new Color(0.15f, 0.12f, 0.08f, 0.35f);
            }
        }

        private void SetNavigationVisible(bool visible)
        {
            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(visible);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(visible);
            }

            if (dotsRoot != null)
            {
                dotsRoot.gameObject.SetActive(visible);
            }
        }

        private int GetLevelCount()
        {
            return levelCatalog != null ? levelCatalog.Count : 0;
        }
    }
}
