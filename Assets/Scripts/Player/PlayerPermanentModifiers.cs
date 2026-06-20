using System;
using System.Collections.Generic;
using Catsss.Configs.Charge;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    /// <summary>Перманентные баффы команды (реплицируются с сервера).</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPermanentModifiers : NetworkBehaviour
    {
        /// <summary>Сервер: уже применённые modifier Id — защита от двойного начисления при replay.</summary>
        private readonly HashSet<byte> _appliedModifierIds = new();

        private readonly NetworkVariable<bool> _canDash = new();
        private readonly NetworkVariable<int> _maxJumps = new(1);
        private readonly NetworkVariable<float> _moveSpeedMultiplier = new(1f);

        public bool CanDash => _canDash.Value;
        public int MaxJumps => _maxJumps.Value;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier.Value;

        public event Action ModifiersChanged;

        public override void OnNetworkSpawn()
        {
            _canDash.OnValueChanged += OnModifierChanged;
            _maxJumps.OnValueChanged += OnMaxJumpsChanged;
            _moveSpeedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
        }

        public override void OnNetworkDespawn()
        {
            _canDash.OnValueChanged -= OnModifierChanged;
            _maxJumps.OnValueChanged -= OnMaxJumpsChanged;
            _moveSpeedMultiplier.OnValueChanged -= OnSpeedMultiplierChanged;
        }

        public void ApplyModifierServer(PermanentModifierDefinition definition)
        {
            if (!IsServer || definition == null)
            {
                return;
            }

            if (!_appliedModifierIds.Add(definition.Id))
            {
                return;
            }

            switch (definition.Kind)
            {
                case PermanentModifierKind.UnlockDash:
                    _canDash.Value = true;
                    break;
                case PermanentModifierKind.UnlockDoubleJump:
                    _maxJumps.Value = Mathf.Max(_maxJumps.Value, 2);
                    break;
                case PermanentModifierKind.MoveSpeedMultiplier:
                    _moveSpeedMultiplier.Value *= definition.Value;
                    break;
                default:
                    Debug.LogWarning($"[PlayerPermanentModifiers] Неизвестный Kind={definition.Kind}.", this);
                    break;
            }
        }

        private void OnModifierChanged(bool previous, bool current) => ModifiersChanged?.Invoke();
        private void OnMaxJumpsChanged(int previous, int current) => ModifiersChanged?.Invoke();
        private void OnSpeedMultiplierChanged(float previous, float current) => ModifiersChanged?.Invoke();
    }
}
