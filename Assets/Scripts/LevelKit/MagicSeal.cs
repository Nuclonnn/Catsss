using System;
using System.Collections;
using System.Collections.Generic;
using Catsss.Core.Events;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Универсальная магическая печать: сервер хранит состояние, а источники активации
    /// (E / trigger / будущие механики) только запрашивают press/release.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MagicSeal : NetworkBehaviour
    {
        [Header("Behaviour")]
        [SerializeField] private MagicSealActivationPolicy activationPolicy = MagicSealActivationPolicy.Momentary;
        [SerializeField, Min(0.02f)] private float interactPulseSeconds = 0.15f;
        [SerializeField] private bool startDisabled;
        [SerializeField] private bool enableDebugLogs;

        [Header("SO Event Channels")]
        [SerializeField] private EmptyEventChannel pressedChannel;
        [SerializeField] private EmptyEventChannel releasedChannel;
        [SerializeField] private EmptyEventChannel oneShotChannel;

        [Header("Local Server Events")]
        [SerializeField] private UnityEvent pressed;
        [SerializeField] private UnityEvent released;
        [SerializeField] private UnityEvent oneShotActivated;

        private readonly NetworkVariable<MagicSealState> _state = new();
        private readonly HashSet<int> _heldSourceIds = new();
        private Coroutine _pulseCoroutine;

        public MagicSealActivationPolicy ActivationPolicy => activationPolicy;
        public MagicSealState State => _state.Value;
        public bool IsPressed => _state.Value == MagicSealState.Pressed;
        public bool IsLocked => _state.Value == MagicSealState.Locked;
        public bool IsDisabled => _state.Value == MagicSealState.Disabled;

        /// <summary>Вызывается на сервере и клиентах при репликации состояния; визуал подписывается сюда.</summary>
        public event Action<MagicSealState, MagicSealState> StateChanged;

        private void Awake()
        {
            _state.OnValueChanged += HandleStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && startDisabled && _state.Value != MagicSealState.Disabled)
            {
                _state.Value = MagicSealState.Disabled;
            }

            StateChanged?.Invoke(_state.Value, _state.Value);
        }

        public override void OnDestroy()
        {
            _state.OnValueChanged -= HandleStateChanged;
            base.OnDestroy();
        }

        /// <summary>Сервер: разовая активация от E или другого импульсного источника.</summary>
        public void PressPulseServer(NetworkPlayerController activator)
        {
            if (!IsServer || !CanAcceptActivation())
            {
                return;
            }

            switch (activationPolicy)
            {
                case MagicSealActivationPolicy.Toggle:
                    SetStateServer(IsPressed ? MagicSealState.Idle : MagicSealState.Pressed, activator);
                    break;
                case MagicSealActivationPolicy.OneShot:
                    SetStateServer(MagicSealState.Locked, activator);
                    break;
                default:
                    StartMomentaryPulseServer(activator);
                    break;
            }
        }

        /// <summary>Сервер: удержание от trigger-источника. sourceId должен быть стабильным для конкретного источника.</summary>
        public void SetHeldBySourceServer(int sourceId, bool isHeld, NetworkPlayerController activator)
        {
            if (!IsServer || sourceId == 0)
            {
                return;
            }

            if (activationPolicy == MagicSealActivationPolicy.Toggle)
            {
                if (isHeld)
                {
                    SetStateServer(IsPressed ? MagicSealState.Idle : MagicSealState.Pressed, activator);
                }

                return;
            }

            if (!CanAcceptActivation())
            {
                return;
            }

            if (activationPolicy == MagicSealActivationPolicy.OneShot)
            {
                if (isHeld)
                {
                    SetStateServer(MagicSealState.Locked, activator);
                }

                return;
            }

            bool changed = isHeld ? _heldSourceIds.Add(sourceId) : _heldSourceIds.Remove(sourceId);

            if (!changed)
            {
                return;
            }

            SetStateServer(_heldSourceIds.Count > 0 ? MagicSealState.Pressed : MagicSealState.Idle, activator);
        }

        /// <summary>Сервер: внешнее включение/выключение печати для будущих пазлов.</summary>
        public void SetDisabledServer(bool disabled)
        {
            if (!IsServer)
            {
                return;
            }

            _heldSourceIds.Clear();
            StopPulseCoroutine();
            SetStateServer(disabled ? MagicSealState.Disabled : MagicSealState.Idle, null);
        }

        /// <summary>Сервер: сброс одноразовой/нажатой печати, если дизайнеру понадобится ручной reset.</summary>
        public void ResetSealServer()
        {
            if (!IsServer)
            {
                return;
            }

            _heldSourceIds.Clear();
            StopPulseCoroutine();
            SetStateServer(MagicSealState.Idle, null);
        }

        private bool CanAcceptActivation()
        {
            return _state.Value != MagicSealState.Disabled && _state.Value != MagicSealState.Locked;
        }

        private void StartMomentaryPulseServer(NetworkPlayerController activator)
        {
            StopPulseCoroutine();
            _pulseCoroutine = StartCoroutine(MomentaryPulseRoutine(activator));
        }

        private IEnumerator MomentaryPulseRoutine(NetworkPlayerController activator)
        {
            SetStateServer(MagicSealState.Pressed, activator);
            yield return new WaitForSeconds(interactPulseSeconds);

            if (IsServer && _heldSourceIds.Count == 0 && _state.Value == MagicSealState.Pressed)
            {
                SetStateServer(MagicSealState.Idle, activator);
            }

            _pulseCoroutine = null;
        }

        private void StopPulseCoroutine()
        {
            if (_pulseCoroutine == null)
            {
                return;
            }

            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        private void SetStateServer(MagicSealState nextState, NetworkPlayerController activator)
        {
            if (!IsServer || _state.Value == nextState)
            {
                return;
            }

            MagicSealState previous = _state.Value;
            _state.Value = nextState;
            RaiseServerSignals(previous, nextState, activator);
        }

        private void RaiseServerSignals(MagicSealState previous, MagicSealState current, NetworkPlayerController activator)
        {
            if (current == MagicSealState.Pressed)
            {
                pressedChannel?.Invoke();
                pressed?.Invoke();
            }
            else if (previous == MagicSealState.Pressed && current == MagicSealState.Idle)
            {
                releasedChannel?.Invoke();
                released?.Invoke();
            }
            else if (current == MagicSealState.Locked)
            {
                oneShotChannel?.Invoke();
                oneShotActivated?.Invoke();
            }

            if (enableDebugLogs)
            {
                string playerInfo = activator != null ? $" by client={activator.OwnerClientId}" : string.Empty;
                Debug.Log($"[MagicSeal] {name}: {previous} -> {current}{playerInfo}", this);
            }
        }

        private void HandleStateChanged(MagicSealState previous, MagicSealState current)
        {
            StateChanged?.Invoke(previous, current);
        }
    }
}
