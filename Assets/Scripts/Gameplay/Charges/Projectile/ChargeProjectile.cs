using System.Collections.Generic;
using Catsss.Configs;
using Catsss.Configs.Charge;
using Catsss.Core.Services;
using Catsss.LevelKit;
using Catsss.Player;
using Catsss.Player.Aim;
using Catsss.Trials;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges.Projectile
{
    /// <summary>
    /// Серверный снаряд заряда: homing, catch/miss, попытки через <see cref="TrialSessionRegistry"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ChargeProjectile : NetworkBehaviour
    {
        private static readonly List<ChargeProjectile> ServerActive = new(16);

        [SerializeField] private bool enableDebugLogs;

        private ulong _throwerClientId;
        private ulong _targetClientId;
        private byte _chargeId;
        private int _trialId;
        private PlayerIncomingChargeIndicator _catcherIncomingIndicator;
        private Vector3 _direction;
        private Transform _targetTransform;
        private float _remainingSnapshot;
        private float _despawnAt;
        private float _throwSpeed;
        private float _homingTurnSpeedDegPerSec;
        private float _homingMinStraightDistance;
        private Vector3 _spawnPosition;
        private bool _isInitialized;
        private bool _resolutionHandled;
        private bool _spawnOverlapChecked;

        /// <summary>Сервер: уничтожить все снаряды испытания без возврата заряда (штраф / отмена trial).</summary>
        public static void AbortAllForTrialServer(int trialId)
        {
            for (int i = ServerActive.Count - 1; i >= 0; i--)
            {
                ChargeProjectile projectile = ServerActive[i];

                if (projectile == null)
                {
                    ServerActive.RemoveAt(i);
                    continue;
                }

                if (projectile._trialId == trialId)
                {
                    projectile.AbortFlightServer("trialAborted");
                }
            }
        }

        /// <summary>
        /// Disconnect: despawn всех снарядов в полёте. Кидавшему, если он ещё в сессии, вернуть заряд без декремента попыток.
        /// </summary>
        public static HashSet<ulong> AbortAllInFlightOnDisconnectServer(ulong disconnectedClientId)
        {
            HashSet<ulong> restoredThrowerIds = new();

            for (int i = ServerActive.Count - 1; i >= 0; i--)
            {
                ChargeProjectile projectile = ServerActive[i];

                if (projectile == null)
                {
                    ServerActive.RemoveAt(i);
                    continue;
                }

                if (projectile.TryAbortInFlightOnDisconnectServer(disconnectedClientId))
                {
                    restoredThrowerIds.Add(projectile._throwerClientId);
                }
            }

            return restoredThrowerIds;
        }

        public void InitializeServer(
            ulong throwerClientId,
            ulong targetClientId,
            byte chargeId,
            int trialId,
            Vector3 direction,
            Transform targetTransform,
            float remainingSnapshot,
            ProjectileSettings settings)
        {
            _throwerClientId = throwerClientId;
            _targetClientId = targetClientId;
            _chargeId = chargeId;
            _trialId = trialId;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            _targetTransform = targetTransform;
            _remainingSnapshot = remainingSnapshot;

            float lifetime = settings != null ? settings.maxLifetime : 4f;
            _throwSpeed = settings != null ? settings.throwSpeed : 12f;
            _homingTurnSpeedDegPerSec = settings != null ? settings.homingTurnSpeedDegPerSec : 110f;
            _homingMinStraightDistance = settings != null ? settings.homingMinStraightDistance : 1.5f;
            _spawnPosition = transform.position;
            _despawnAt = Time.time + lifetime;
            _isInitialized = true;
            _resolutionHandled = false;
            _spawnOverlapChecked = false;

            transform.rotation = Quaternion.LookRotation(_direction);
            ConfigureRigidbody();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            if (!_isInitialized)
            {
                if (IsScenePlacedNetworkObject())
                {
                    Debug.LogWarning(
                        "[ChargeProjectile] In-scene объект без InitializeServer — отключён. "
                        + "Удали ChargeProjectileRoot из иерархии Sandbox.",
                        this);
                    enabled = false;
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("[ChargeProjectile] Динамический spawn без InitializeServer — despawn.", this);
                    DespawnServer();
                }

                return;
            }

            RegisterActiveServer();
            TryRegisterIncomingTelegraphServer();
        }

        public override void OnNetworkDespawn()
        {
            TryUnregisterIncomingTelegraphServer();
            UnregisterActiveServer();
        }

        private void FixedUpdate()
        {
            if (!IsServer || !_isInitialized || _resolutionHandled)
            {
                return;
            }

            if (!_spawnOverlapChecked)
            {
                _spawnOverlapChecked = true;

                if (TryResolveSpawnInsideSolid())
                {
                    return;
                }
            }

            if (Time.time >= _despawnAt)
            {
                HandleMissServer("timeout");
                return;
            }

            ApplyHomingStep();
            transform.position += _direction * (_throwSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void ApplyHomingStep()
        {
            if (_targetClientId == ulong.MaxValue)
            {
                return;
            }

            if (_targetTransform == null)
            {
                _targetTransform = ProjectileHomingTarget.ResolveTransform(_targetClientId);
            }

            if (_targetTransform == null)
            {
                return;
            }

            float traveled = Vector3.Distance(_spawnPosition, transform.position);

            if (traveled < _homingMinStraightDistance)
            {
                return;
            }

            if (!ProjectileHomingTarget.TryGetWorldPosition(_targetTransform, out Vector3 targetPosition))
            {
                return;
            }

            _direction = ProjectileTrajectorySimulator.ApplyHomingStep(
                _direction,
                transform.position,
                targetPosition,
                _homingTurnSpeedDegPerSec,
                Time.fixedDeltaTime);
        }

        /// <summary>Сервер: промах от антимаг-барьера с BlocksProjectiles (как обычный environment miss).</summary>
        public void TryHandleBarrierMissServer(string reason)
        {
            if (!IsServer || !_isInitialized || _resolutionHandled)
            {
                return;
            }

            HandleMissServer(reason);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !_isInitialized || _resolutionHandled || other == null)
            {
                return;
            }

            if (TryResolveAntiMagicZone(other, out AntiMagicZone antiMagicZone))
            {
                if (antiMagicZone.IsZoneActive && antiMagicZone.BlocksProjectiles)
                {
                    HandleMissServer("antiMagic");
                }

                return;
            }

            if (ShouldIgnoreCollider(other))
            {
                return;
            }

            if (TryResolveCatcher(other, out PlayerChargeController catcher))
            {
                HandleCatchServer(catcher);
                return;
            }

            if (IsThrowerCollider(other))
            {
                return;
            }

            if (!ShouldCountAsEnvironmentMiss(other))
            {
                return;
            }

            HandleMissServer($"hit:{other.name}");
        }

        private bool TryResolveSpawnInsideSolid()
        {
            float radius = ResolveTriggerRadius();

            Collider[] overlaps = Physics.OverlapSphere(
                transform.position,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider other = overlaps[i];

                if (other == null || ShouldIgnoreCollider(other) || IsThrowerCollider(other))
                {
                    continue;
                }

                if (TryResolveCatcher(other, out _))
                {
                    continue;
                }

                HandleMissServer("spawnInsideGeometry");
                return true;
            }

            return false;
        }

        private float ResolveTriggerRadius()
        {
            if (TryGetComponent(out SphereCollider sphere))
            {
                float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                return sphere.radius * scale;
            }

            return 0.35f;
        }

        private void HandleCatchServer(PlayerChargeController catcher)
        {
            if (_resolutionHandled || !CanResolveGameplayForTrial())
            {
                AbortFlightServer("catchAfterTrialEnded");
                return;
            }

            _resolutionHandled = true;

            float fullDuration = ResolveCatchDuration();
            bool applied = catcher.TryApplyChargeWithRemainingServer(_chargeId, _trialId, fullDuration);

            if (applied && catcher.TryGetComponent(out PlayerAimController aimController))
            {
                aimController.NotifyThrowCatchCooldownStarted();
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[ChargeProjectile] Catch → clientId={catcher.OwnerClientId}, applied={applied}, "
                    + $"chargeId={_chargeId}, trialId={_trialId}, duration={fullDuration:F1}s",
                    this);
            }

            DespawnServer();
        }

        private void HandleMissServer(string reason)
        {
            if (_resolutionHandled)
            {
                return;
            }

            if (!CanResolveGameplayForTrial())
            {
                AbortFlightServer($"missIgnored:{reason}");
                return;
            }

            _resolutionHandled = true;

            bool penaltyApplied = false;
            bool restored = false;

            if (_trialId > 0 && ServiceLocator.TryGet(out TrialSessionRegistry registry))
            {
                bool attemptsExhausted = registry.TryRegisterThrowMissServer(_trialId);

                if (attemptsExhausted)
                {
                    registry.ApplyTeamTrialPenalty(_trialId, TrialPenaltyReason.MissedTooManyTimes);
                    penaltyApplied = true;
                }
                else
                {
                    restored = TryRestoreChargeToThrowerServer(ComputeRestoredDurationAfterMiss());
                }
            }
            else
            {
                restored = TryRestoreChargeToThrowerServer(ComputeRestoredDurationAfterMiss());
            }

            if (enableDebugLogs)
            {
                int attemptsLeft = _trialId > 0 && ServiceLocator.TryGet(out TrialSessionRegistry reg)
                    ? reg.GetThrowAttemptsRemaining(_trialId)
                    : -1;

                Debug.Log(
                    $"[ChargeProjectile] Miss ({reason}) → thrower={_throwerClientId}, restored={restored}, "
                    + $"penalty={penaltyApplied}, attemptsLeft={attemptsLeft}",
                    this);
            }

            DespawnServer();
        }

        private bool CanResolveGameplayForTrial()
        {
            if (_trialId <= 0)
            {
                return true;
            }

            return ServiceLocator.TryGet(out TrialSessionRegistry registry)
                   && registry.IsTrialActive(_trialId);
        }

        private void AbortFlightServer(string reason)
        {
            if (_resolutionHandled)
            {
                return;
            }

            _resolutionHandled = true;
            TryUnregisterIncomingTelegraphServer();

            if (enableDebugLogs)
            {
                Debug.Log($"[ChargeProjectile] Abort ({reason}) trialId={_trialId}", this);
            }

            DespawnServer();
        }

        /// <summary>Disconnect: без TryRegisterThrowMiss; snapshot без бонуса промаха.</summary>
        private bool TryAbortInFlightOnDisconnectServer(ulong disconnectedClientId)
        {
            if (_resolutionHandled)
            {
                return false;
            }

            _resolutionHandled = true;
            TryUnregisterIncomingTelegraphServer();

            bool restored = false;
            bool throwerStillConnected = _throwerClientId != disconnectedClientId
                                         && IsClientConnected(_throwerClientId);

            if (throwerStillConnected && _chargeId != 0 && CanResolveGameplayForTrial())
            {
                restored = TryRestoreChargeToThrowerServer(_remainingSnapshot);

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[ChargeProjectile] Disconnect abort → restored={restored}, thrower={_throwerClientId}, "
                        + $"remaining={_remainingSnapshot:F1}s",
                        this);
                }
            }

            DespawnServer();
            return restored;
        }

        private static bool IsClientConnected(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            return networkManager != null && networkManager.ConnectedClients.ContainsKey(clientId);
        }

        private float ResolveCatchDuration()
        {
            return TryResolveChargeDefinition(out ChargeTypeDefinition definition)
                ? definition.DurationSeconds
                : Mathf.Max(_remainingSnapshot, 0f);
        }

        private float ComputeRestoredDurationAfterMiss()
        {
            return TryResolveChargeDefinition(out ChargeTypeDefinition definition)
                ? definition.GetRestoredDurationAfterMiss(_remainingSnapshot)
                : 0f;
        }

        private bool TryResolveChargeDefinition(out ChargeTypeDefinition definition)
        {
            definition = null;

            if (!ServiceLocator.TryGet(out TrialSessionRegistry registry)
                || registry.ContentCatalog == null
                || registry.ContentCatalog.ChargeTypes == null)
            {
                return false;
            }

            return registry.ContentCatalog.ChargeTypes.TryGet(_chargeId, out definition);
        }

        private bool TryRestoreChargeToThrowerServer(float remainingDuration)
        {
            if (_chargeId == 0)
            {
                return false;
            }

            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager == null)
            {
                return false;
            }

            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                if (client.ClientId != _throwerClientId || client.PlayerObject == null)
                {
                    continue;
                }

                if (!client.PlayerObject.TryGetComponent(out PlayerChargeController throwerCharge))
                {
                    return false;
                }

                return throwerCharge.TryApplyChargeWithRemainingServer(_chargeId, _trialId, remainingDuration);
            }

            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || !_isInitialized || _resolutionHandled || collision == null)
            {
                return;
            }

            Collider other = collision.collider;

            if (other == null)
            {
                return;
            }

            if (TryResolveAntiMagicZone(other, out AntiMagicZone antiMagicZone)
                && antiMagicZone.IsZoneActive
                && antiMagicZone.BlocksProjectiles)
            {
                HandleMissServer("antiMagic");
            }
        }

        private static bool TryResolveAntiMagicZone(Collider other, out AntiMagicZone zone)
        {
            zone = other.GetComponentInParent<AntiMagicZone>();
            return zone != null;
        }

        private static bool ShouldIgnoreCollider(Collider other)
        {
            if (TryResolveAntiMagicZone(other, out AntiMagicZone antiMagicZone)
                && (!antiMagicZone.IsZoneActive || !antiMagicZone.BlocksProjectiles))
            {
                return true;
            }

            if (other.GetComponentInParent<TrialBoundsZone>() != null)
            {
                return true;
            }

            if (other.GetComponentInParent<TrialFinishZone>() != null)
            {
                return true;
            }

            if (other.GetComponentInParent<TrialPylonStart>() != null)
            {
                return true;
            }

            return false;
        }

        private static bool ShouldCountAsEnvironmentMiss(Collider other) => !other.isTrigger;

        private bool TryResolveCatcher(Collider other, out PlayerChargeController catcher)
        {
            catcher = other.GetComponentInParent<PlayerChargeController>();

            if (catcher == null || catcher.OwnerClientId == _throwerClientId)
            {
                return false;
            }

            return true;
        }

        private bool IsThrowerCollider(Collider other)
        {
            NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();

            return networkObject != null
                   && networkObject.OwnerClientId == _throwerClientId;
        }

        private void TryRegisterIncomingTelegraphServer()
        {
            if (_targetClientId == ulong.MaxValue
                || !PlayerIncomingChargeIndicator.TryGetForClient(_targetClientId, out PlayerIncomingChargeIndicator indicator))
            {
                return;
            }

            _catcherIncomingIndicator = indicator;
            indicator.RegisterIncomingThrowServer(_throwerClientId);
        }

        private void TryUnregisterIncomingTelegraphServer()
        {
            if (_catcherIncomingIndicator == null)
            {
                return;
            }

            _catcherIncomingIndicator.UnregisterIncomingThrowServer();
            _catcherIncomingIndicator = null;
        }

        private void RegisterActiveServer()
        {
            if (!ServerActive.Contains(this))
            {
                ServerActive.Add(this);
            }
        }

        private void UnregisterActiveServer()
        {
            ServerActive.Remove(this);
        }

        private void ConfigureRigidbody()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
        }

        private void DespawnServer()
        {
            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                return;
            }

            bool destroy = !IsScenePlacedNetworkObject();
            NetworkObject.Despawn(destroy);
        }

        private bool IsScenePlacedNetworkObject()
        {
            return NetworkObject != null
                   && NetworkObject.IsSceneObject.HasValue
                   && NetworkObject.IsSceneObject.Value;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_isInitialized)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _direction * 1.5f);
        }
    }
}
