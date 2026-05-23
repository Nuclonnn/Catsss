using System.Collections.Generic;
using Catsss.Core.Services;
using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Trials
{
    /// <summary>
    /// Объём зоны испытания (trigger). Выход любого игрока во время активного trial → командный штраф на сервере.
    /// Падение в пропасть = выход из этого объёма (коллайдер зоны не должен включать пустоту под уровнем).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TrialBoundsZone : MonoBehaviour
    {
        [SerializeField] private TrialDefinition trial;
        [SerializeField] private TrialPylonStart linkedPylon;
        [SerializeField] private bool drawBoundsGizmo = true;
        [SerializeField] private Color gizmoColor = new(1f, 0.85f, 0.1f, 0.35f);

        private readonly HashSet<ulong> _networkObjectIdsInside = new();

        private int TrialId => trial != null ? trial.TrialId : 0;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServerAuthority() || !TryGetPlayerNetworkObjectId(other, out ulong networkObjectId))
            {
                return;
            }

            _networkObjectIdsInside.Add(networkObjectId);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServerAuthority() || !TryGetPlayerNetworkObjectId(other, out ulong networkObjectId))
            {
                return;
            }

            if (!_networkObjectIdsInside.Remove(networkObjectId))
            {
                return;
            }

            if (TrialId <= 0)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out TrialSessionRegistry registry) || !registry.IsTrialActive(TrialId))
            {
                return;
            }

            registry.ApplyTeamTrialPenalty(TrialId, TrialPenaltyReason.LeftTrialBounds);
        }

        private static bool IsServerAuthority()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            return networkManager != null && networkManager.IsServer;
        }

        private static bool TryGetPlayerNetworkObjectId(Collider other, out ulong networkObjectId)
        {
            networkObjectId = 0;

            if (other == null)
            {
                return false;
            }

            NetworkObject networkObject = other.GetComponentInParent<NetworkObject>();

            // Только сетевые игроки (PlayerRoot), не мусорные коллайдеры сцены.
            if (networkObject == null || !networkObject.TryGetComponent(out NetworkPlayerController _))
            {
                return false;
            }

            networkObjectId = networkObject.NetworkObjectId;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawBoundsGizmo)
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
                case CapsuleCollider capsule:
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (trial == null || linkedPylon == null)
            {
                return;
            }

            if (linkedPylon.TrialId != trial.TrialId)
            {
                Debug.LogWarning(
                    $"[TrialBoundsZone] linkedPylon trialId={linkedPylon.TrialId} не совпадает с definition={trial.TrialId}.",
                    this);
            }
        }
#endif
    }
}
