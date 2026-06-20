using System;

using Catsss.Gameplay.Charges.Projectile;

using Catsss.Configs;

using Catsss.Core.Timing;

using Unity.Netcode;

using UnityEngine;



namespace Catsss.Player.Aim

{

    /// <summary>

    /// Режим прицеливания (ПКМ toggle). Не FSM-стейт: слой ввода + будущий визуал.

    /// ЛКМ (Throw) обрабатывается только пока <see cref="IsAiming"/> — иначе ввод не потребляется.

    /// </summary>

    [DisallowMultipleComponent]

    [RequireComponent(typeof(NetworkPlayerController))]

    [RequireComponent(typeof(PlayerInputReader))]

    [RequireComponent(typeof(PlayerChargeController))]

    public sealed class PlayerAimController : NetworkBehaviour

    {

        [Header("Config")]

        [SerializeField] private GameConfig gameConfig;



        [Header("References")]

        [SerializeField] private NetworkPlayerController playerController;

        [SerializeField] private PlayerInputReader inputReader;

        [SerializeField] private PlayerChargeController chargeController;

        [SerializeField] private PlayerCameraController playerCamera;

        [SerializeField] private PlayerThrowOrigin throwOrigin;

        [SerializeField] private NetworkObject chargeProjectilePrefab;



        [Header("Throw Validation")]

        [SerializeField, Min(0.1f)] private float maxOriginDistanceFromPlayer = 3f;



        [Header("Debug")]

        [SerializeField] private bool enableDebugLogs;



        private readonly CooldownTimer _aimEnterCooldown = new(0f);

        private NetworkPlayerController _currentTarget;

        private bool _isAiming;



        public bool IsAiming => _isAiming;



        public NetworkPlayerController CurrentTarget => _currentTarget;



        /// <summary>Есть ли живой напарник для homing (solo-тест: false, бросок всё равно разрешён).</summary>

        public bool HasThrowTarget => _currentTarget != null;



        /// <summary>OwnerClientId цели или 0 — нет напарника (прямой полёт по камере).</summary>

        public ulong CurrentTargetClientId => _currentTarget != null ? _currentTarget.OwnerClientId : 0;



        public event Action AimEntered;



        public event Action AimExited;



        public event Action<NetworkPlayerController> TargetChanged;



        /// <summary>Владелец отправил бросок на сервер (origin, direction, targetClientId).</summary>

        public event Action<Vector3, Vector3, ulong> ThrowCommitted;



        private void Reset()

        {

            playerController = GetComponent<NetworkPlayerController>();

            inputReader = GetComponent<PlayerInputReader>();

            chargeController = GetComponent<PlayerChargeController>();

            playerCamera = GetComponentInChildren<PlayerCameraController>(true);

            throwOrigin = GetComponentInChildren<PlayerThrowOrigin>(true);

        }



        public override void OnNetworkSpawn()

        {

            if (!IsOwner)

            {

                enabled = false;

                return;

            }



            chargeController.ChargeChanged += OnChargeChanged;

        }



        public override void OnNetworkDespawn()

        {

            if (chargeController != null)

            {

                chargeController.ChargeChanged -= OnChargeChanged;

            }



            if (_isAiming)

            {

                ForceExitAim(silent: true);

            }

        }



        private void Update()

        {

            if (!IsOwner)

            {

                return;

            }



            TickAimEnterCooldown();



            if (inputReader.ConsumeAimToggledThisFrame())

            {

                if (_isAiming)

                {

                    ExitAim();

                }

                else

                {

                    TryEnterAim();

                }

            }



            if (!_isAiming)

            {

                return;

            }



            RefreshTargetIfNeeded();



            if (ConsumeThrowPressedWhileAiming())

            {

                TryCommitThrow();

            }

        }



        /// <summary>Единственная точка потребления Throw/Attack — только в Aim.</summary>

        public bool ConsumeThrowPressedWhileAiming()

        {

            if (!_isAiming || inputReader == null)

            {

                return false;

            }



            return inputReader.ConsumeThrowPressedThisFrame();

        }



        /// <summary>Вызывается после поимки заряда (шаг 4.3+) — блокирует повторный вход в Aim.</summary>

        public void NotifyThrowCatchCooldownStarted()

        {

            float cooldown = 0.4f;

            if (chargeController != null && chargeController.ActiveDefinition != null)
            {
                cooldown = chargeController.ActiveDefinition.ThrowCooldownAfterCatch;
            }

            _aimEnterCooldown.Restart(cooldown);

        }



        private void TryCommitThrow()

        {

            if (chargeController == null || chargeController.ChargeId == 0)

            {

                return;

            }



            if (chargeProjectilePrefab == null)

            {

                Debug.LogWarning("[PlayerAim] Не назначен chargeProjectilePrefab.", this);

                return;

            }



            Vector3 origin = GetThrowOriginWorld();

            Vector3 direction = GetThrowDirection();

            ulong targetClientId = CurrentTargetClientId;



            ThrowChargeServerRpc(origin, direction, targetClientId);

            ThrowCommitted?.Invoke(origin, direction, targetClientId);



            if (enableDebugLogs)

            {

                Debug.Log(

                    $"[PlayerAim] ThrowCommitted → targetClientId={targetClientId}, origin={origin}, dir={direction}",

                    this);

            }

        }



        private Vector3 GetThrowDirection()

        {

            ProjectileSettings settings = gameConfig != null ? gameConfig.Projectile : new ProjectileSettings();

            Vector3 origin = GetThrowOriginWorld();



            if (_isAiming && playerCamera != null && playerCamera.HasCachedAimDirection)

            {

                return playerCamera.CachedAimDirection;

            }



            Camera camera = playerCamera != null ? playerCamera.UnityCamera : null;

            return PlayerThrowDirectionResolver.Resolve(

                camera,

                origin,

                settings,

                transform);

        }



        private Vector3 GetThrowOriginWorld()

        {

            return throwOrigin != null

                ? throwOrigin.WorldPosition

                : transform.position + Vector3.up * 1.2f;

        }



        [Rpc(SendTo.Server)]

        private void ThrowChargeServerRpc(Vector3 origin, Vector3 direction, ulong targetClientId, RpcParams rpcParams = default)

        {

            if (rpcParams.Receive.SenderClientId != OwnerClientId)

            {

                return;

            }



            if (chargeProjectilePrefab == null || gameConfig == null)

            {

                return;

            }



            if (chargeController == null || !chargeController.TryTakeChargeForThrowServer(

                    out byte chargeId,

                    out int trialId,

                    out float remainingSnapshot))

            {

                return;

            }



            float maxOriginDistance = maxOriginDistanceFromPlayer;

            float originSqr = (origin - transform.position).sqrMagnitude;



            if (originSqr > maxOriginDistance * maxOriginDistance)

            {

                origin = throwOrigin != null ? throwOrigin.WorldPosition : transform.position + Vector3.up * 1.2f;

            }



            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

            float forwardOffset = gameConfig.Projectile.spawnForwardOffset;
            origin += direction * forwardOffset;

            Transform targetTransform = ResolveTargetTransform(targetClientId);

            NetworkObject spawned = Instantiate(chargeProjectilePrefab, origin, Quaternion.LookRotation(direction));



            if (!spawned.TryGetComponent(out ChargeProjectile projectile))

            {

                Destroy(spawned.gameObject);

                return;

            }



            projectile.InitializeServer(

                OwnerClientId,

                targetClientId,

                chargeId,

                trialId,

                direction,

                targetTransform,

                remainingSnapshot,

                gameConfig.Projectile);



            spawned.Spawn(true);

        }



        private static Transform ResolveTargetTransform(ulong targetClientId)

        {

            if (targetClientId == 0)

            {

                return null;

            }



            NetworkManager networkManager = NetworkManager.Singleton;



            if (networkManager == null)

            {

                return null;

            }



            foreach (NetworkClient client in networkManager.ConnectedClientsList)

            {

                if (client.ClientId != targetClientId || client.PlayerObject == null)

                {

                    continue;

                }



                return client.PlayerObject.transform;

            }



            return null;

        }



        private void TryEnterAim()

        {

            if (!CanEnterAim(out string blockReason))

            {

                LogBlockedEnter(blockReason);

                return;

            }



            PlayerAimTargetResolver.TryResolveTarget(playerController, out NetworkPlayerController target);



            _isAiming = true;

            SetTarget(target);



            if (enableDebugLogs)

            {

                if (target != null)

                {

                    Debug.Log($"[PlayerAim] AimEntered → target clientId={target.OwnerClientId}", this);

                }

                else

                {

                    Debug.Log("[PlayerAim] AimEntered → без цели (solo / тест)", this);

                }

            }



            AimEntered?.Invoke();

            ProjectileThrowSignals.RaiseAimModeChanged(OwnerClientId, true);

        }



        private void ExitAim()

        {

            if (!_isAiming)

            {

                return;

            }



            ForceExitAim(silent: false);

        }



        private void ForceExitAim(bool silent)

        {

            _isAiming = false;

            SetTarget(null);



            if (!silent)

            {

                if (enableDebugLogs)

                {

                    Debug.Log("[PlayerAim] AimExited", this);

                }



                AimExited?.Invoke();

                ProjectileThrowSignals.RaiseAimModeChanged(OwnerClientId, false);

            }

        }



        private bool CanEnterAim(out string blockReason)

        {

            blockReason = null;



            if (chargeController == null || chargeController.ChargeId == 0)

            {

                blockReason = "нет заряда";

                return false;

            }



            if (_aimEnterCooldown.IsRunning)

            {

                blockReason = "кулдаун после поимки";

                return false;

            }



            if (!inputReader.IsAimActionBound)

            {

                blockReason = "action Aim не настроен";

                return false;

            }



            return true;

        }



        private void RefreshTargetIfNeeded()

        {

            PlayerAimTargetResolver.TryResolveTarget(playerController, out NetworkPlayerController target);



            if (target != _currentTarget)

            {

                SetTarget(target);

            }

        }



        private void SetTarget(NetworkPlayerController target)

        {

            if (_currentTarget == target)

            {

                return;

            }



            _currentTarget = target;

            TargetChanged?.Invoke(target);

        }



        private void OnChargeChanged(byte chargeId, int trialId)

        {

            if (chargeId == 0 && _isAiming)

            {

                if (enableDebugLogs)

                {

                    Debug.Log("[PlayerAim] Заряд сброшен — авто-выход из Aim", this);

                }



                ExitAim();

            }

        }



        private void TickAimEnterCooldown()

        {

            if (_aimEnterCooldown.IsRunning)

            {

                _aimEnterCooldown.Tick(Time.deltaTime);

            }

        }



        private void LogBlockedEnter(string reason)

        {

            if (enableDebugLogs)

            {

                Debug.Log($"[PlayerAim] Вход в Aim заблокирован: {reason}", this);

            }

        }

    }

}


