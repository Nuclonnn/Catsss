using System;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges.Projectile
{
    /// <summary>
    /// Сетевой флаг «в этого игрока летит снаряд». Логика без VFX/анимаций.
    /// Визуал/аудио подписываются на события или на <see cref="ProjectileThrowSignals"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIncomingChargeIndicator : NetworkBehaviour
    {
        private readonly NetworkVariable<ulong> _incomingFromClientId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private int _serverIncomingCount;

        /// <summary>OwnerClientId кидавшего или 0 — ничего не летит.</summary>
        public ulong IncomingFromClientId => _incomingFromClientId.Value;

        public bool HasIncomingCharge => _incomingFromClientId.Value != 0;

        /// <summary>Вкл/выкл telegraph (удобно для VFX/Animator bool).</summary>
        public event Action<bool> IsIncomingTargetChanged;

        /// <summary>Новый thrower id (0 = сброс). Для смены цвета/иконки по типу заряда позже.</summary>
        public event Action<ulong> IncomingThrowerClientIdChanged;

        public override void OnNetworkSpawn()
        {
            _incomingFromClientId.OnValueChanged += OnIncomingFromClientIdChanged;
            ulong initial = _incomingFromClientId.Value;
            PublishFromValue(initial, notifyBoolChanged: initial != 0);
        }

        public override void OnNetworkDespawn()
        {
            _incomingFromClientId.OnValueChanged -= OnIncomingFromClientIdChanged;
        }

        /// <summary>Сервер: снаряд начал полёт к этому игроку.</summary>
        public void RegisterIncomingThrowServer(ulong throwerClientId)
        {
            if (!IsServer || throwerClientId == 0)
            {
                return;
            }

            _serverIncomingCount++;
            _incomingFromClientId.Value = throwerClientId;
        }

        /// <summary>Сервер: один снаряд перестал целиться в этого игрока.</summary>
        public void UnregisterIncomingThrowServer()
        {
            if (!IsServer)
            {
                return;
            }

            _serverIncomingCount = Mathf.Max(0, _serverIncomingCount - 1);

            if (_serverIncomingCount == 0)
            {
                _incomingFromClientId.Value = 0;
            }
        }

        public static bool TryGetForClient(ulong clientId, out PlayerIncomingChargeIndicator indicator)
        {
            indicator = null;
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return false;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                if (client.ClientId != clientId || client.PlayerObject == null)
                {
                    continue;
                }

                return client.PlayerObject.TryGetComponent(out indicator);
            }

            return false;
        }

        private void OnIncomingFromClientIdChanged(ulong previousValue, ulong newValue)
        {
            bool wasIncoming = previousValue != 0;
            bool isIncoming = newValue != 0;

            PublishFromValue(newValue, notifyBoolChanged: wasIncoming != isIncoming);
        }

        private void PublishFromValue(ulong throwerClientId, bool notifyBoolChanged)
        {
            if (notifyBoolChanged)
            {
                IsIncomingTargetChanged?.Invoke(throwerClientId != 0);
            }

            IncomingThrowerClientIdChanged?.Invoke(throwerClientId);
            ProjectileThrowSignals.RaiseIncomingProjectileTargetChanged(OwnerClientId, throwerClientId);
        }
    }
}
