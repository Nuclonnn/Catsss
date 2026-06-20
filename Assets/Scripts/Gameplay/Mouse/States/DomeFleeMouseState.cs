using Catsss.Core.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace Catsss.Gameplay.Mouse.States
{
    /// <summary>Купол: NavMesh flee, поимка через declarative transition в Caught.</summary>
    internal sealed class DomeFleeMouseState : IState
    {
        private readonly IMouseBrainStateHost _host;

        public DomeFleeMouseState(IMouseBrainStateHost host)
        {
            _host = host;
        }

        public void OnEnter()
        {
            _host.TransitionSignals.ClearPendingRouteEnd();
            _host.SetRouteFollowingServer(false);
            _host.SetDomeFleeActiveServer(true);
            _host.SetPresenceModeServer(MousePresenceMode.Physical);
            _host.NotifyMaterializedClients();

            NavMeshAgent agent = _host.Agent;

            if (agent == null)
            {
                Debug.LogWarning("[MouseBrain] NavMeshAgent не назначен — flee в куполе не работает.");
                _host.SetDomeFleeActiveServer(false);
                return;
            }

            if (!_host.TryPlaceAgentOnNavMeshServer(_host.Transform.position, out _))
            {
                Debug.LogWarning(
                    $"[MouseBrain] NavMesh не найден рядом с мышью ({_host.Transform.position}). " +
                    $"Запеките NavMesh под куполом и/или поставьте Home Burrow на платформе. " +
                    $"Радиус поиска: {_host.DomeNavMeshSampleRadius} м.");
                _host.ShutdownNavMeshAgentServer();
                _host.SetDomeFleeActiveServer(false);
                return;
            }

            float speed = _host.Config != null ? _host.Config.DomeAgentSpeed : 3.5f;
            agent.speed = speed;
            _host.ConfigureNavMeshAgentForManualSyncServer();

            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        public void OnExit()
        {
            _host.SetDomeFleeActiveServer(false);
            _host.ShutdownNavMeshAgentServer();
        }

        public void OnUpdate() { }

        public void OnFixedUpdate()
        {
            NavMeshAgent agent = _host.Agent;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            Transform closestPlayer = _host.FindClosestPlayerTransform();

            if (closestPlayer == null)
            {
                return;
            }

            float fleeDistance = _host.Config != null ? _host.Config.DomeFleeDistance : 6f;
            Vector3 fleeDirection = (_host.Transform.position - closestPlayer.position).normalized;
            fleeDirection.y = 0f;

            if (fleeDirection.sqrMagnitude < 0.0001f)
            {
                fleeDirection = _host.Transform.forward;
            }

            Vector3 target = _host.Transform.position + fleeDirection.normalized * fleeDistance;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            _host.SyncNavMeshAgentTransformServer();
        }
    }
}
