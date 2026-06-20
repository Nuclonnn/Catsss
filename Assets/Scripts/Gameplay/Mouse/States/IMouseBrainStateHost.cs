using Catsss.Configs.Mouse;
using Catsss.Core.FSM;
using Catsss.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Catsss.Gameplay.Mouse.States
{
    /// <summary>Контракт для mouse FSM states — без Unity Visuals.</summary>
    internal interface IMouseBrainStateHost
    {
        Transform Transform { get; }

        MouseConfig Config { get; }

        MouseBrainTransitionSignals TransitionSignals { get; }

        float DomeNavMeshSampleRadius { get; }

        NavMeshAgent Agent { get; }

        void TeleportToHomeServer();

        void TeleportServer(Vector3 position, Quaternion rotation);

        void MoveServer(Vector3 position, Quaternion rotation);

        void SetPresenceModeServer(MousePresenceMode mode);

        void SetRouteFollowingServer(bool isFollowing);

        void SetDomeFleeActiveServer(bool isActive);

        void EnableNavMeshAgentServer(bool enabled);

        void ShutdownNavMeshAgentServer();

        Transform FindClosestPlayerTransform();

        bool TryPlaceAgentOnNavMeshServer(Vector3 nearPosition, out Vector3 placedPosition);

        void SyncNavMeshAgentTransformServer();

        void ConfigureNavMeshAgentForManualSyncServer();

        void NotifyMaterializedClients();

        void NotifyCaughtServer();
    }
}
