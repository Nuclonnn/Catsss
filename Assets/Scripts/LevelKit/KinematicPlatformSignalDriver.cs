using UnityEngine;

namespace Catsss.LevelKit
{
    /// <summary>Подписывается на EmptyEventChannel и переводит сигналы в команды KinematicPlatform (server-only).</summary>
    [DisallowMultipleComponent]
    public sealed class KinematicPlatformSignalDriver : EmptyEventChannelSignalDriverBase
    {
        [Header("Setup")]
        [SerializeField] private KinematicPlatformSignalDriverPreset preset = KinematicPlatformSignalDriverPreset.Custom;
        [SerializeField] private KinematicPlatform platform;

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
            ResolvePlatformReference();
        }

        private void Awake()
        {
            ResolvePlatformReference();
        }

        protected override void OnEnable()
        {
            ResolvePlatformReference();
            WarnIfDuplicateChannels();
            base.OnEnable();
        }

        protected override void HandlePressedServer()
        {
            if (platform == null || onPressed == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onPressed, pressedTravelMode);
        }

        protected override void HandleReleasedServer()
        {
            if (platform == null || onReleased == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onReleased, releasedTravelMode);
        }

        protected override void HandleOneShotServer()
        {
            if (platform == null || onOneShot == KinematicPlatformSignalAction.None)
            {
                return;
            }

            platform.ApplySignalAction(onOneShot, oneShotTravelMode);
        }

        private void ResolvePlatformReference()
        {
            if (platform == null)
            {
                platform = GetComponent<KinematicPlatform>();
            }
        }

        private void WarnIfDuplicateChannels()
        {
            if (PressedChannel == null)
            {
                return;
            }

            if ((ReleasedChannel != null && PressedChannel == ReleasedChannel)
                || (OneShotChannel != null && PressedChannel == OneShotChannel)
                || (ReleasedChannel != null && OneShotChannel != null && ReleasedChannel == OneShotChannel))
            {
                Debug.LogWarning(
                    $"[KinematicPlatformSignalDriver] {name}: один EmptyEventChannel назначен на несколько слотов. "
                    + "Каждый сигнал вызовет все Actions сразу. Раздели каналы или оставь One Shot пустым.",
                    this);
            }
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
    }
}
