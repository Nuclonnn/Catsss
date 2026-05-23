using System;
using Catsss.Core.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace Catsss.Interaction
{
    [Serializable]
    public sealed class InteractionPromptSettings
    {
        [SerializeField] private LocalizedTextReference text = new();

        public LocalizedTextReference Text => text;
        public LocalizedString LocalizedText => text.LocalizedText;
        public string EditorFallback => text.EditorFallback;

        public bool HasLocalization => text.HasLocalization;

        public string ResolveDisplayText() => text.ResolveDisplayText();
    }
}
