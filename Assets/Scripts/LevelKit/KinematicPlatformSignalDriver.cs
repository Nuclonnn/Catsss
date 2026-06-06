using Catsss.Core.Events;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и переводит сигналы в команды KinematicPlatform (server-only).</summary>
    [DisallowMultipleComponent]
    public sealed class KinematicPlatformSignalDriver : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private KinematicPlatformSignalDriverPreset preset = KinematicPlatformSignalDriverPreset.Custom;
        [SerializeField] private KinematicPlatform platform;

        [Header("Channels")]
        [SerializeField] private EmptyEventChannel pressedChannel;
        [SerializeField] private EmptyEventChannel releasedChannel;
        [SerializeField] private EmptyEventChannel oneShotChannel;

        [Header("Actions")]
        [SerializeField] private KinematicPlatformSignalAction onPressed = KinematicPlatformSignalAction.PlayForward;
        [SerializeField] private KinematicPlatformSignalAction onReleased = KinematicPlatformSignalAction.None;
        [SerializeField] private KinematicPlatformSignalAction onOneShot = KinematicPlatformSignalAction.PlayForward;

        [Header("Travel")]
        [SerializeField] private KinematicPlatformSignalTravelMode pressedTravelMode = KinematicPlatformSignalTravelMode.OneWay;
        [SerializeField] private KinematicPlatformSignalTravelMode releasedTravelMode = KinematicPlatformSignalTravelMode.OneWay;
        [SerializeField] private KinematicPlatformSignalTravelMode oneShotTravelMode = KinematicPlatformSignalTravelMode.OneWay;

        public KinematicPlatformSignalDriverPreset Preset => preset;

        private void Reset()
        {
            if (platform == null)
            {
                platform = GetComponent<KinematicPlatform>();
            }
        }

        private void Awake()
        {
            if (platform == null)
            {
                platform = GetComponent<KinematicPlatform>();
            }
        }

        private void OnEnable()
        {
            if (platform == null)
            {
                platform = GetComponent<KinematicPlatform>();
            }

            WarnIfDuplicateChannels();
            SubscribeChannels();
        }

        private void OnDisable()
        {
            UnsubscribeChannels();
        }

        private void SubscribeChannels()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised += OnPressed;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised += OnReleased;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised += OnOneShot;
            }
        }

        private void UnsubscribeChannels()
        {
            if (pressedChannel != null)
            {
                pressedChannel.Raised -= OnPressed;
            }

            if (releasedChannel != null)
            {
                releasedChannel.Raised -= OnReleased;
            }

            if (oneShotChannel != null)
            {
                oneShotChannel.Raised -= OnOneShot;
            }
        }

        private void WarnIfDuplicateChannels()
        {
            if (pressedChannel == null)
            {
                return;
            }

            if ((releasedChannel != null && pressedChannel == releasedChannel)
                || (oneShotChannel != null && pressedChannel == oneShotChannel)
                || (releasedChannel != null && oneShotChannel != null && releasedChannel == oneShotChannel))
            {
                Debug.LogWarning(
                    $"[KinematicPlatformSignalDriver] {name}: один EmptyEventChannel назначен на несколько слотов. "
                    + "Каждый сигнал вызовет все Actions сразу. Раздели каналы или оставь One Shot пустым.",
                    this);
            }
        }

        private void OnPressed(EmptyEvent _)
        {
            if (!IsServerAuthority() || platform == null || onPressed == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onPressed, pressedTravelMode);
        }

        private void OnReleased(EmptyEvent _)
        {
            if (!IsServerAuthority() || platform == null || onReleased == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onReleased, releasedTravelMode);
        }

        private void OnOneShot(EmptyEvent _)
        {
            if (!IsServerAuthority() || platform == null || onOneShot == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onOneShot, oneShotTravelMode);
        }

        /// <summary>Editor: заполнить поля по пресету. Custom не трогает значения.</summary>
        public void ApplyPresetValues(KinematicPlatformSignalDriverPreset presetToApply)
        {
            preset = presetToApply;

            if (presetToApply == KinematicPlatformSignalDriverPreset.Custom)
            {
                return;
            }

            onReleased = KinematicPlatformSignalAction.None;
            onOneShot = KinematicPlatformSignalAction.None;
            pressedTravelMode = KinematicPlatformSignalTravelMode.OneWay;
            releasedTravelMode = KinematicPlatformSignalTravelMode.OneWay;
            oneShotTravelMode = KinematicPlatformSignalTravelMode.OneWay;

            switch (presetToApply)
            {
                case KinematicPlatformSignalDriverPreset.ToggleBridge:
                case KinematicPlatformSignalDriverPreset.HoldElevator:
                    onPressed = KinematicPlatformSignalAction.PlayForward;
                    onReleased = KinematicPlatformSignalAction.PlayReverse;
                    break;
                case KinematicPlatformSignalDriverPreset.OneShotGate:
                    onOneShot = KinematicPlatformSignalAction.PlayForward;
                    oneShotTravelMode = KinematicPlatformSignalTravelMode.OneWay;
                    onPressed = KinematicPlatformSignalAction.None;
                    break;
                case KinematicPlatformSignalDriverPreset.OneShotStartYoyo:
                    onOneShot = KinematicPlatformSignalAction.PlayForward;
                    oneShotTravelMode = KinematicPlatformSignalTravelMode.Yoyo;
                    onPressed = KinematicPlatformSignalAction.None;
                    break;
                case KinematicPlatformSignalDriverPreset.ToggleDirection:
                    onPressed = KinematicPlatformSignalAction.ToggleDirection;
                    pressedTravelMode = KinematicPlatformSignalTravelMode.Yoyo;
                    break;
            }
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }
    }
}
