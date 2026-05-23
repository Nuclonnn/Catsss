using System;
using Catsss.Configs;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.Charges
{
    [RequireComponent(typeof(NetworkObject), typeof(Collider))]
    public sealed class HomingChargeProjectile : NetworkBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private LayerMask environmentMask = ~0;

        private ulong _throwerClientId;
        private int _chargeId;
        private Vector3 _direction;
        private Transform _target;
        private float _despawnAt;

        public event Action<Vector3> Fizzled;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        public void Initialize(ulong throwerClientId, int chargeId, Vector3 direction, Transform target)
        {
            _throwerClientId = throwerClientId;
            _chargeId = chargeId;
            _direction = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            _target = target;

            float lifetime = gameConfig != null ? gameConfig.Projectile.lifetime : 4f;
            _despawnAt = Time.time + lifetime;
        }

        private void FixedUpdate()
        {
            if (!IsServer)
            {
                return;
            }

            if (Time.time >= _despawnAt)
            {
                Fizzle(transform.position);
                return;
            }

            if (_target != null)
            {
                Vector3 toTarget = (_target.position - transform.position).normalized;
                float turnSpeed = gameConfig != null ? gameConfig.Projectile.homingTurnSpeed : 9f;
                _direction = Vector3.RotateTowards(_direction, toTarget, turnSpeed * Time.fixedDeltaTime, 0f).normalized;
            }

            float speed = gameConfig != null ? gameConfig.Projectile.speed : 15f;
            transform.position += _direction * speed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder != null)
            {
                if (chargeHolder.OwnerClientId != _throwerClientId)
                {
                    chargeHolder.GrantChargeServer(_chargeId);
                    DespawnProjectile();
                }

                return;
            }

            if (((1 << other.gameObject.layer) & environmentMask) != 0)
            {
                Fizzle(transform.position);
            }
        }

        public void Fizzle(Vector3 position)
        {
            PlayFizzleClientRpc(position);
            DespawnProjectile();
        }

        [ClientRpc]
        private void PlayFizzleClientRpc(Vector3 position)
        {
            Fizzled?.Invoke(position);
        }

        private void DespawnProjectile()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }
    }
}
