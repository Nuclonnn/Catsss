using Catsss.Configs;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Player.Aim
{
    /// <summary>
    /// Визуал Aim Mode: дуга траектории + жёлтый контур напарника.
    /// Траектория обновляется после Cinemachine (не в раннем LateUpdate).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerAimController))]
    public sealed class PlayerAimVisualsPresenter : NetworkBehaviour
    {
        [SerializeField] private PlayerAimController aimController;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private PlayerThrowOrigin throwOrigin;
        [SerializeField] private PlayerCameraController playerCamera;
        [SerializeField] private LineRendererAimTrajectory trajectoryRenderer;
        [SerializeField] private PlayerTargetMarker targetMarker;

        private void Reset()
        {
            aimController = GetComponent<PlayerAimController>();
            throwOrigin = GetComponentInChildren<PlayerThrowOrigin>(true);
            playerCamera = GetComponentInChildren<PlayerCameraController>(true);
            trajectoryRenderer = GetComponentInChildren<LineRendererAimTrajectory>(true);
            targetMarker = GetComponentInChildren<PlayerTargetMarker>(true);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                HideAll();
                return;
            }

            aimController.AimEntered += OnAimEntered;
            aimController.AimExited += OnAimExited;
            aimController.TargetChanged += OnTargetChanged;
            SubscribeCameraEvents();
            HideAll();
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeCameraEvents();

            if (aimController != null)
            {
                aimController.AimEntered -= OnAimEntered;
                aimController.AimExited -= OnAimExited;
                aimController.TargetChanged -= OnTargetChanged;
            }

            HideAll();
        }

        private void OnAimEntered()
        {
            trajectoryRenderer?.Show();
            RefreshAimAfterCamera();
            RefreshTargetMarker();
        }

        private void OnAimExited()
        {
            HideAll();
        }

        private void OnTargetChanged(NetworkPlayerController target)
        {
            RefreshTargetMarker();
            RefreshAimAfterCamera();
        }

        private void SubscribeCameraEvents()
        {
            UnsubscribeCameraEvents();

            if (playerCamera == null)
            {
                return;
            }

            playerCamera.AimDirectionUpdated += OnCameraAimUpdated;
        }

        private void UnsubscribeCameraEvents()
        {
            if (playerCamera != null)
            {
                playerCamera.AimDirectionUpdated -= OnCameraAimUpdated;
            }
        }

        private void OnCameraAimUpdated()
        {
            if (!IsOwner || aimController == null || !aimController.IsAiming)
            {
                return;
            }

            RefreshAimAfterCamera();
        }

        private void RefreshAimAfterCamera()
        {
            ProjectileSettings settings = gameConfig != null ? gameConfig.Projectile : new ProjectileSettings();
            Vector3 originWorld = GetThrowOriginWorld();

            playerCamera?.RefreshAimDirectionCache(originWorld, settings, transform);
            RefreshTrajectory();
        }

        private void RefreshTrajectory()
        {
            if (trajectoryRenderer == null)
            {
                return;
            }

            ProjectileSettings projectile = gameConfig != null ? gameConfig.Projectile : new ProjectileSettings();
            Vector3 originWorld = GetThrowOriginWorld();
            Vector3 direction = ResolveThrowDirection();

            float forwardOffset = projectile.spawnForwardOffset;
            Vector3 flightOrigin = originWorld + direction * forwardOffset;

            Transform targetTransform = aimController.CurrentTarget != null
                ? aimController.CurrentTarget.transform
                : null;

            trajectoryRenderer.UpdateTrajectory(originWorld, flightOrigin, direction, targetTransform);
        }

        private void RefreshTargetMarker()
        {
            if (targetMarker == null)
            {
                return;
            }

            NetworkPlayerController target = aimController.CurrentTarget;

            if (target == null)
            {
                targetMarker.Hide();
                return;
            }

            Camera camera = playerCamera != null ? playerCamera.UnityCamera : null;
            targetMarker.Show(target, camera);
        }

        private Vector3 GetThrowOriginWorld()
        {
            return throwOrigin != null
                ? throwOrigin.WorldPosition
                : transform.position + Vector3.up * 1.2f;
        }

        private Vector3 ResolveThrowDirection()
        {
            if (playerCamera != null && playerCamera.HasCachedAimDirection)
            {
                return playerCamera.CachedAimDirection;
            }

            ProjectileSettings settings = gameConfig != null ? gameConfig.Projectile : new ProjectileSettings();
            Camera camera = playerCamera != null ? playerCamera.UnityCamera : null;

            return PlayerThrowDirectionResolver.Resolve(
                camera,
                GetThrowOriginWorld(),
                settings,
                transform);
        }

        private void HideAll()
        {
            trajectoryRenderer?.Hide();
            targetMarker?.Hide();
        }
    }
}
