using Catsss.Player;
using UnityEngine;

namespace Catsss.Interaction
{
    /// <summary>Любой объект уровня, с которым игрок взаимодействует по E (пилоны, рычаги, сыр и т.д.).</summary>
    public interface IInteractable
    {
        Transform PromptAnchor { get; }
        InteractionPromptSettings PromptSettings { get; }
        bool CanInteract(NetworkPlayerController interactor);
        void RequestInteract(NetworkPlayerController interactor);
    }
}
