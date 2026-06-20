using Catsss.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Базовый signal driver: подписка на pressed/released/oneShot EmptyEventChannel.
    /// Наследники реализуют server-only обработку сигналов.
    /// </summary>
    public abstract class EmptyEventChannelSignalDriverBase : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EmptyEventChannel pressedChannel;
        [SerializeField] private EmptyEventChannel releasedChannel;
        [SerializeField] private EmptyEventChannel oneShotChannel;

        protected EmptyEventChannel PressedChannel => pressedChannel;

        protected EmptyEventChannel ReleasedChannel => releasedChannel;

        protected EmptyEventChannel OneShotChannel => oneShotChannel;

        protected virtual void OnEnable()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised += HandlePressedRaised;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised += HandleReleasedRaised;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised += HandleOneShotRaised;
            }
        }

        protected virtual void OnDisable()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised -= HandlePressedRaised;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised -= HandleReleasedRaised;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised -= HandleOneShotRaised;
            }
        }

        protected abstract void HandlePressedServer();

        protected abstract void HandleReleasedServer();

        protected abstract void HandleOneShotServer();

        protected static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }

        private void HandlePressedRaised(EmptyEvent _)
        {
            if (IsServerAuthority())
            {
                HandlePressedServer();
            }
        }

        private void HandleReleasedRaised(EmptyEvent _)
        {
            if (IsServerAuthority())
            {
                HandleReleasedServer();
            }
        }

        private void HandleOneShotRaised(EmptyEvent _)
        {
            if (IsServerAuthority())
            {
                HandleOneShotServer();
            }
        }
    }
}
