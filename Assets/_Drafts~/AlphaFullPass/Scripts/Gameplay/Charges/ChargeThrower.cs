using Catsss.Configs;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges
{
    [RequireComponent(typeof(PlayerController), typeof(PlayerChargeHolder))]
    public sealed class ChargeThrower : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private NetworkObject projectilePrefab;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private LayerMask playerMask = ~0;

        private PlayerController _playerController;
        private PlayerChargeHolder _chargeHolder;
        private PlayerChargeHolder _currentTarget;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _chargeHolder = GetComponent<PlayerChargeHolder>();
        }

        public override void OnNetworkSpawn()
        {
            if (_playerController != null)
            {
                _playerController.AimStarted += RefreshSoftLock;
                _playerController.ThrowRequested += ThrowCurrentCharge;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_playerController != null)
            {
                _playerController.AimStarted -= RefreshSoftLock;
                _playerController.ThrowRequested -= ThrowCurrentCharge;
            }
        }

        private void Update()
        {
            if (IsOwner && _playerController != null && _playerController.IsAiming)
            {
                RefreshSoftLock();
            }
        }

        private void RefreshSoftLock()
        {
            if (!IsOwner || _chargeHolder == null || !_chargeHolder.HasCharge)
            {
                _currentTarget = null;
                return;
            }

            _currentTarget = FindBestTarget();
        }

        private void ThrowCurrentCharge()
        {
            if (!IsOwner || _chargeHolder == null || !_chargeHolder.HasCharge)
            {
                return;
            }

            Transform origin = throwOrigin != null ? throwOrigin : transform;
            Vector3 direction = origin.forward;
            ulong targetNetworkId = _currentTarget != null ? _currentTarget.NetworkObjectId : 0;
            ThrowChargeServerRpc(_chargeHolder.CurrentChargeId, targetNetworkId, origin.position, direction);
        }

        [ServerRpc]
        private void ThrowChargeServerRpc(int chargeId, ulong targetNetworkId, Vector3 origin, Vector3 direction)
        {
            if (projectilePrefab == null || _chargeHolder == null || _chargeHolder.CurrentChargeId != chargeId)
            {
                return;
            }

            NetworkObject spawned = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction.normalized));
            HomingChargeProjectile projectile = spawned.GetComponent<HomingChargeProjectile>();
            Transform target = TryResolveTarget(targetNetworkId);
            projectile.Initialize(OwnerClientId, chargeId, direction.normalized, target);
            spawned.Spawn(true);
            _chargeHolder.ClearChargeServer(false);
        }

        private PlayerChargeHolder FindBestTarget()
        {
            Transform origin = throwOrigin != null ? throwOrigin : transform;
            float radius = gameConfig != null ? gameConfig.Projectile.softLockRadius : 10f;
            float maxAngle = gameConfig != null ? gameConfig.Projectile.softLockAngle : 55f;
            Collider[] hits = Physics.OverlapSphere(origin.position, radius, playerMask, QueryTriggerInteraction.Ignore);

            PlayerChargeHolder best = null;
            float bestScore = float.MaxValue;

            foreach (Collider hit in hits)
            {
                PlayerChargeHolder candidate = hit.GetComponentInParent<PlayerChargeHolder>();
                if (candidate == null || candidate == _chargeHolder)
                {
                    continue;
                }

                Vector3 toCandidate = candidate.transform.position - origin.position;
                float angle = Vector3.Angle(origin.forward, toCandidate);
                if (angle > maxAngle)
                {
                    continue;
                }

                float score = angle + toCandidate.magnitude * 0.1f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private Transform TryResolveTarget(ulong targetNetworkId)
        {
            if (targetNetworkId == 0 || NetworkManager == null)
            {
                return null;
            }

            return NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObject)
                ? targetObject.transform
                : null;
        }
    }
}
