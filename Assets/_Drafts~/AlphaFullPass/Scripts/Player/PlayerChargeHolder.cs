using System;
using Catsss.Configs;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player
{
    public sealed class PlayerChargeHolder : NetworkBehaviour
    {
        [SerializeField] private ChargeDatabase chargeDatabase;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform respawnPoint;

        private readonly NetworkVariable<int> _currentChargeId = new();
        private readonly NetworkVariable<float> _chargeNormalized = new();
        private float _remainingTime;

        public event Action<int> ChargeChanged;
        public event Action<float> ChargeTimerChanged;

        public int CurrentChargeId => _currentChargeId.Value;
        public float ChargeNormalized => _chargeNormalized.Value;
        public bool HasCharge => CurrentChargeId > 0;
        public Transform RespawnPoint => respawnPoint;

        public override void OnNetworkSpawn()
        {
            _currentChargeId.OnValueChanged += HandleChargeChanged;
            _chargeNormalized.OnValueChanged += HandleChargeTimerChanged;
            ApplyChargeLocally(_currentChargeId.Value);
        }

        public override void OnNetworkDespawn()
        {
            _currentChargeId.OnValueChanged -= HandleChargeChanged;
            _chargeNormalized.OnValueChanged -= HandleChargeTimerChanged;
        }

        private void Update()
        {
            if (!IsServer || _currentChargeId.Value <= 0)
            {
                return;
            }

            ChargeType chargeType = chargeDatabase != null ? chargeDatabase.GetById(_currentChargeId.Value) : null;
            float duration = chargeType != null ? chargeType.DurationTime : 1f;
            _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
            _chargeNormalized.Value = duration <= 0f ? 0f : _remainingTime / duration;

            if (_remainingTime <= 0f)
            {
                ClearChargeServer(false);
            }
        }

        public void SetRespawnPoint(Transform point)
        {
            respawnPoint = point;
        }

        public void RequestGrantCharge(int chargeId)
        {
            if (IsServer)
            {
                GrantChargeServer(chargeId);
                return;
            }

            GrantChargeServerRpc(chargeId);
        }

        public void RequestClearCharge(bool teleportToRespawn)
        {
            if (IsServer)
            {
                ClearChargeServer(teleportToRespawn);
                return;
            }

            ClearChargeServerRpc(teleportToRespawn);
        }

        public void GrantChargeServer(int chargeId)
        {
            if (!IsServer)
            {
                return;
            }

            ChargeType chargeType = chargeDatabase != null ? chargeDatabase.GetById(chargeId) : null;
            _currentChargeId.Value = chargeType != null ? chargeType.Id : 0;
            _remainingTime = chargeType != null ? chargeType.DurationTime : 0f;
            _chargeNormalized.Value = chargeType != null ? 1f : 0f;
        }

        public void ClearChargeServer(bool teleportToRespawn)
        {
            if (!IsServer)
            {
                return;
            }

            _currentChargeId.Value = 0;
            _remainingTime = 0f;
            _chargeNormalized.Value = 0f;

            if (teleportToRespawn && respawnPoint != null)
            {
                TeleportOwnerClientRpc(respawnPoint.position);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void GrantChargeServerRpc(int chargeId)
        {
            GrantChargeServer(chargeId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClearChargeServerRpc(bool teleportToRespawn)
        {
            ClearChargeServer(teleportToRespawn);
        }

        [ClientRpc]
        private void TeleportOwnerClientRpc(Vector3 position)
        {
            if (IsOwner && playerController != null)
            {
                playerController.Teleport(position);
            }
        }

        private void HandleChargeChanged(int previousValue, int newValue)
        {
            ApplyChargeLocally(newValue);
            ChargeChanged?.Invoke(newValue);
        }

        private void HandleChargeTimerChanged(float previousValue, float newValue)
        {
            ChargeTimerChanged?.Invoke(newValue);
        }

        private void ApplyChargeLocally(int chargeId)
        {
            ChargeType chargeType = chargeDatabase != null ? chargeDatabase.GetById(chargeId) : null;

            if (playerController != null)
            {
                playerController.ApplyCharge(chargeType);
            }
        }
    }
}
