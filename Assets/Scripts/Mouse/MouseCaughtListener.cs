using Catsss.Core.Events;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Заглушка для проверки MouseCaughtChannel до GameFlowManager (Stage 7).</summary>
    public sealed class MouseCaughtListener : MonoBehaviour
    {
        [SerializeField] private EmptyEventChannel mouseCaughtChannel;

        [Header("Debug")]
        [SerializeField] private bool logToConsole = true;

        private void OnEnable()
        {
            if (mouseCaughtChannel != null)
            {
                mouseCaughtChannel.Raised += OnMouseCaught;
            }
        }

        private void OnDisable()
        {
            if (mouseCaughtChannel != null)
            {
                mouseCaughtChannel.Raised -= OnMouseCaught;
            }
        }

        private void OnMouseCaught(EmptyEvent _)
        {
            if (logToConsole)
            {
                Debug.Log("[MouseCaughtListener] Мышь поймана — MouseCaughtChannel получен.", this);
            }
        }
    }
}
