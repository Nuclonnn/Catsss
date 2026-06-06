using System;
using System.Collections.Generic;
using Catsss.Charges.Projectile;
using Catsss.Core.Services;
using Catsss.Player;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Catsss.LevelKit
{
    /// <summary>
    /// Антимагическая зона: штрафует заряженного игрока в активном trial (телепорт + попытка броска, заряд остаётся).
    /// Trigger — завеса/туман; solid BoxCollider — поверхность/препятствие. Незаряженных не затрагивает.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AntiMagicZone : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool zoneStartsEnabled = true;
        [SerializeField] private bool blocksProjectiles;

        [Header("Respawn")]
        [Tooltip("Куда телепортировать заряженного игрока после штрафа (рядом с зоной).")]
        [SerializeField] private Transform respawnPoint;

        [Header("Events (server)")]
        [SerializeField] private UnityEvent chargedPlayerPenalized;
        [SerializeField] private UnityEvent projectileBlocked;

        [Header("Debug")]
        [SerializeField] private bool drawZoneGizmo = true;
        [SerializeField] private Color gizmoColor = new(0.65f, 0.2f, 1f, 0.35f);
        [SerializeField] private bool enableDebugLogs;

        private readonly HashSet<ulong> _chargedPlayersInside = new();
        private bool _isZoneActive;

        /// <summary>Логика штрафа/снаряда активна (коллайдер остаётся включённым).</summary>
        public bool IsZoneActive => _isZoneActive;

        /// <summary>Снаряд останавливается в этой зоне, если зона активна.</summary>
        public bool BlocksProjectiles => blocksProjectiles;

        public event Action<bool> ZoneActiveChanged;
        public event Action<bool> BlocksProjectilesChanged;
        public event Action<NetworkPlayerController> ChargedPlayerPenalized;
        public event Action<ChargeProjectile> ProjectileBlocked;

        private void Awake()
        {
            _isZoneActive = zoneStartsEnabled;
        }

        private void Start()
        {
            ZoneActiveChanged?.Invoke(_isZoneActive);
        }

        /// <summary>Сервер: runtime-состояние от signal driver.</summary>
        public void SetRuntimeStateServer(bool zoneActive, bool blockProjectiles)
        {
            if (!IsServerAuthority())
            {
                return;
            }

            bool zoneChanged = _isZoneActive != zoneActive;
            bool blockChanged = blocksProjectiles != blockProjectiles;

            _isZoneActive = zoneActive;
            blocksProjectiles = blockProjectiles;

            if (!_isZoneActive)
            {
                _chargedPlayersInside.Clear();
            }

            if (zoneChanged)
            {
                ZoneActiveChanged?.Invoke(_isZoneActive);
            }

            if (blockChanged)
            {
                BlocksProjectilesChanged?.Invoke(blocksProjectiles);
            }
        }

        /// <summary>Клиенты: синхронизация визуала (gameplay только на сервере).</summary>
        public void ApplyVisualStateClient(bool zoneActive, bool blockProjectiles)
        {
            bool zoneChanged = _isZoneActive != zoneActive;
            _isZoneActive = zoneActive;
            blocksProjectiles = blockProjectiles;

            if (zoneChanged)
            {
                ZoneActiveChanged?.Invoke(_isZoneActive);
            }
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null && col is BoxCollider)
            {
                // Trigger-завеса по умолчанию; для пола снимите isTrigger вручную.
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!UsesTriggerContact())
            {
                return;
            }

            ProcessContactEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!UsesTriggerContact())
            {
                return;
            }

            ProcessContactExit(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (UsesTriggerContact() || collision == null)
            {
                return;
            }

            ProcessContactEnter(collision.collider);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (UsesTriggerContact() || collision == null)
            {
                return;
            }

            ProcessContactExit(collision.collider);
        }

        private void ProcessContactEnter(Collider other)
        {
            if (!IsServerAuthority() || other == null || !_isZoneActive)
            {
                return;
            }

            if (TryResolveProjectile(other, out ChargeProjectile projectile))
            {
                HandleProjectileContact(projectile);
                return;
            }

            if (!TryResolveChargedPlayerInActiveTrial(other, out NetworkPlayerController player, out int trialId))
            {
                return;
            }

            ulong networkObjectId = player.NetworkObject.NetworkObjectId;

            if (!_chargedPlayersInside.Add(networkObjectId))
            {
                return;
            }

            ApplyPlayerPenalty(player, trialId);
        }

        private void ProcessContactExit(Collider other)
        {
            if (!IsServerAuthority() || !TryGetPlayerNetworkObjectId(other, out ulong networkObjectId))
            {
                return;
            }

            _chargedPlayersInside.Remove(networkObjectId);
        }

        private void HandleProjectileContact(ChargeProjectile projectile)
        {
            if (!blocksProjectiles || projectile == null)
            {
                return;
            }

            projectile.TryHandleBarrierMissServer("antiMagic");
            projectileBlocked?.Invoke();
            ProjectileBlocked?.Invoke(projectile);

            if (enableDebugLogs)
            {
                Debug.Log($"[AntiMagicZone] {name}: projectile blocked.", this);
            }
        }

        private void ApplyPlayerPenalty(NetworkPlayerController player, int trialId)
        {
            if (!ServiceLocator.TryGet(out TrialSessionRegistry registry))
            {
                return;
            }

            bool attemptsExhausted = registry.TryRegisterThrowMissServer(trialId);

            if (attemptsExhausted)
            {
                registry.ApplyTeamTrialPenalty(trialId, TrialPenaltyReason.AntiMagicZone);

                if (enableDebugLogs)
                {
                    Debug.Log($"[AntiMagicZone] {name}: attempts exhausted → team penalty trialId={trialId}.", this);
                }

                return;
            }

            TeleportPlayerNearZone(player);
            chargedPlayerPenalized?.Invoke();
            ChargedPlayerPenalized?.Invoke(player);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[AntiMagicZone] {name}: player penalized trialId={trialId}, charge kept, attempts left="
                    + $"{registry.GetThrowAttemptsRemaining(trialId)}.",
                    this);
            }
        }

        private void TeleportPlayerNearZone(NetworkPlayerController player)
        {
            if (player == null)
            {
                return;
            }

            Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position + transform.forward * 2f;
            Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : player.transform.rotation;
            player.TeleportFromServerClientRpc(position, rotation);
        }

        private static bool TryResolveChargedPlayerInActiveTrial(
            Collider other,
            out NetworkPlayerController player,
            out int trialId)
        {
            player = null;
            trialId = 0;

            if (!TryGetPlayerNetworkObjectId(other, out _))
            {
                return false;
            }

            player = other.GetComponentInParent<NetworkPlayerController>();

            if (player == null || !player.TryGetComponent(out PlayerChargeController chargeController))
            {
                return false;
            }

            if (!chargeController.HasActiveCharge)
            {
                return false;
            }

            trialId = chargeController.ActiveTrialId;

            if (trialId <= 0)
            {
                return false;
            }

            if (!ServiceLocator.TryGet(out TrialSessionRegistry registry) || !registry.IsTrialActive(trialId))
            {
                return false;
            }

            return true;
        }

        private static bool TryResolveProjectile(Collider other, out ChargeProjectile projectile)
        {
            projectile = other.GetComponentInParent<ChargeProjectile>();
            return projectile != null;
        }

        private static bool TryGetPlayerNetworkObjectId(Collider other, out ulong networkObjectId)
        {
            networkObjectId = 0;

            if (other == null)
            {
                return false;
            }

            NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();

            if (networkObject == null || !networkObject.TryGetComponent(out NetworkPlayerController _))
            {
                return false;
            }

            networkObjectId = networkObject.NetworkObjectId;
            return true;
        }

        private bool UsesTriggerContact()
        {
            Collider col = GetComponent<Collider>();
            return col != null && col.isTrigger;
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawZoneGizmo)
            {
                return;
            }

            Collider col = GetComponent<Collider>();

            if (col == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider box:
                    Gizmos.DrawCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    break;
            }

            if (respawnPoint != null)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawSphere(respawnPoint.position, 0.2f);
                Gizmos.DrawLine(transform.position, respawnPoint.position);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (respawnPoint == null)
            {
                Transform existing = transform.Find("RespawnPoint");

                if (existing != null)
                {
                    respawnPoint = existing;
                }
            }
        }
#endif
    }
}
