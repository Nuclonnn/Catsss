using System.Collections.Generic;
using Catsss.Core.Events;
using Catsss.Core.FSM;
using Catsss.Core.Path;
using UnityEngine;

namespace Catsss.Gameplay.Mouse.States
{
    internal sealed class RouteFollowMouseState : IState
    {
        private readonly IMouseBrainStateHost _host;
        private readonly WaypointPathFollower _follower = new();

        private MouseRoute _route;
        private bool _teleportToRouteStart;
        private bool _routeFinishedHandled;
        private Vector3 _lastPosition;
        private EmptyEventChannel _onStartedChannel;
        private EmptyEventChannel _onFinishedChannel;
        private readonly HashSet<int> _firedWaypointEventIndices = new();
        private bool _isWaitingAtWaypoint;
        private float _waypointWaitRemaining;

        public RouteFollowMouseState(IMouseBrainStateHost host)
        {
            _host = host;
        }

        public void Prepare(
            MouseRoute route,
            bool teleportToRouteStart,
            EmptyEventChannel onStartedChannel,
            EmptyEventChannel onFinishedChannel)
        {
            _route = route;
            _teleportToRouteStart = teleportToRouteStart;
            _onStartedChannel = onStartedChannel;
            _onFinishedChannel = onFinishedChannel;
        }

        public void OnEnter()
        {
            _routeFinishedHandled = false;
            _isWaitingAtWaypoint = false;
            _waypointWaitRemaining = 0f;
            _firedWaypointEventIndices.Clear();
            _host.EnableNavMeshAgentServer(false);
            _host.SetRouteFollowingServer(true);
            _host.SetPresenceModeServer(MousePresenceMode.Spectral);

            Vector3[] points = _route.GetWaypointPositions();
            int startIndex = 0;

            if (_teleportToRouteStart)
            {
                _host.TeleportServer(_route.GetStartPosition(), _route.GetStartRotation());
            }
            else
            {
                startIndex = WaypointPathSnapshot.FindClosestWaypointIndex(points, _host.Transform.position);
            }

            _follower.Begin(points, startIndex);
            _lastPosition = _host.Transform.position;
            _onStartedChannel?.Invoke();
            BeginWaypointWaitIfNeeded(TriggerWaypointEventsServer(0));
        }

        public void OnExit()
        {
            _host.SetRouteFollowingServer(false);
            _host.TransitionSignals.ClearPendingRouteEnd();
        }

        public void OnUpdate() { }

        public void OnFixedUpdate()
        {
            if (_route == null)
            {
                return;
            }

            if (_follower.IsComplete)
            {
                HandleRouteFinishedOnce();
                return;
            }

            if (_isWaitingAtWaypoint)
            {
                _waypointWaitRemaining -= Time.fixedDeltaTime;

                if (_waypointWaitRemaining > 0f)
                {
                    return;
                }

                _isWaitingAtWaypoint = false;
                _waypointWaitRemaining = 0f;
            }

            float speed = _route.ResolveSpeed(_host.Config);
            bool reachedWaypoint = _follower.TryAdvance(
                Time.fixedDeltaTime,
                speed,
                out Vector3 nextPosition,
                out int reachedWaypointIndex);

            Quaternion rotation = ResolveFacingRotation(nextPosition);
            _host.MoveServer(nextPosition, rotation);
            _lastPosition = nextPosition;

            if (reachedWaypoint)
            {
                BeginWaypointWaitIfNeeded(TriggerWaypointEventsServer(reachedWaypointIndex));
            }

            if (_follower.IsComplete)
            {
                HandleRouteFinishedOnce();
            }
        }

        private float TriggerWaypointEventsServer(int waypointIndex)
        {
            System.ReadOnlySpan<MouseRouteWaypointEvent> events = _route.WaypointEvents;
            float maxWait = 0f;

            for (int i = 0; i < events.Length; i++)
            {
                MouseRouteWaypointEvent waypointEvent = events[i];

                if (waypointEvent == null || waypointEvent.Channel == null)
                {
                    continue;
                }

                if (waypointEvent.WaypointIndex != waypointIndex)
                {
                    continue;
                }

                if (waypointEvent.PlayOncePerRouteRun && _firedWaypointEventIndices.Contains(i))
                {
                    continue;
                }

                waypointEvent.Channel.Invoke();
                _firedWaypointEventIndices.Add(i);
                maxWait = Mathf.Max(maxWait, waypointEvent.WaitSeconds);

                if (_route.LogWaypointEvents)
                {
                    Debug.Log(
                        $"[MouseRoute] {_route.name}: waypoint P{waypointIndex} -> {waypointEvent.Channel.name}",
                        _route);
                }
            }

            return maxWait;
        }

        private void BeginWaypointWaitIfNeeded(float waitSeconds)
        {
            if (waitSeconds <= 0f)
            {
                return;
            }

            _isWaitingAtWaypoint = true;
            _waypointWaitRemaining = Mathf.Max(_waypointWaitRemaining, waitSeconds);
        }

        private void HandleRouteFinishedOnce()
        {
            if (_routeFinishedHandled)
            {
                return;
            }

            _routeFinishedHandled = true;
            _onFinishedChannel?.Invoke();

            switch (_route.EndMode)
            {
                case MouseRouteEndMode.ReturnHidden:
                    _host.TransitionSignals.SetPendingRouteEnd(
                        MouseRouteEndMode.ReturnHidden,
                        hiddenAtCurrentPosition: true);
                    break;
                case MouseRouteEndMode.EnterDome:
                    _host.TransitionSignals.SetPendingRouteEnd(MouseRouteEndMode.EnterDome);
                    break;
                case MouseRouteEndMode.StopAtEnd:
                default:
                    _host.SetRouteFollowingServer(false);
                    break;
            }
        }

        private Quaternion ResolveFacingRotation(Vector3 nextPosition)
        {
            Vector3 delta = nextPosition - _lastPosition;
            delta.y = 0f;

            if (delta.sqrMagnitude < 0.0001f)
            {
                return _host.Transform.rotation;
            }

            Quaternion target = Quaternion.LookRotation(delta.normalized, Vector3.up);
            float turnSpeed = _host.Config != null ? _host.Config.RouteTurnSpeedDegPerSec : 720f;
            return Quaternion.RotateTowards(
                _host.Transform.rotation,
                target,
                turnSpeed * Time.fixedDeltaTime);
        }
    }
}
