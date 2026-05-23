using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Catsss.Core.Localization
{
    /// <summary>
    /// Screen-space текст (TMP или Legacy Text), обновляется при смене локали.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalizedUiText : MonoBehaviour
    {
        [SerializeField] private LocalizedTextReference textReference = new();
        [SerializeField] private TMP_Text tmpText;
        [SerializeField] private Text legacyText;

        public LocalizedTextReference TextReference => textReference;

        private void Awake()
        {
            ResolveTargets();
        }

        private void OnEnable()
        {
            ResolveTargets();
            textReference.Bind(ApplyText);
        }

        private void OnDisable()
        {
            textReference.Unbind();
        }

        private void ResolveTargets()
        {
            if (tmpText == null)
            {
                tmpText = GetComponent<TMP_Text>();
            }

            if (legacyText == null)
            {
                legacyText = GetComponent<Text>();
            }
        }

        private void ApplyText(string value)
        {
            if (tmpText != null)
            {
                tmpText.text = value ?? string.Empty;
            }

            if (legacyText != null)
            {
                legacyText.text = value ?? string.Empty;
            }
        }
    }
}
