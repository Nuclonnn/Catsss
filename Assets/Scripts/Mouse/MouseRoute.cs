using System;
using Catsss.Configs.Mouse;
using Catsss.Core.Path;
using UnityEngine;

namespace Catsss.Gameplay.Mouse
{
    /// <summary>Сценический маршрут мыши: дочерние Point_* под Path, скорость и поведение в конце.</summary>
    public sealed class MouseRoute : MonoBehaviour
    {
        [Header("Path")]
        [Tooltip("Дочерний Path с Point_0, Point_1... Порядок детей = порядок маршрута.")]
        [SerializeField] private Transform pathRoot;

        [Header("Motion")]
        [Tooltip("0 = взять скорость из MouseConfig на мыши.")]
        [SerializeField, Min(0f)] private float speedOverride;

        [SerializeField] private MouseRouteEndMode endMode = MouseRouteEndMode.StopAtEnd;

        [Header("Waypoint Events")]
        [SerializeField] private MouseRouteWaypointEvent[] waypointEvents = Array.Empty<MouseRouteWaypointEvent>();

        [Header("Debug")]
        [SerializeField] private bool drawPathGizmo = true;
        [SerializeField] private bool logWaypointEvents;

        public Transform PathRoot => pathRoot;
        public MouseRouteEndMode EndMode => endMode;
        public bool DrawPathGizmo => drawPathGizmo;
        public bool LogWaypointEvents => logWaypointEvents;
        public ReadOnlySpan<MouseRouteWaypointEvent> WaypointEvents => waypointEvents;

        public float ResolveSpeed(MouseConfig config)
        {
            if (speedOverride > 0f)
            {
                return speedOverride;
            }

            return config != null ? config.RouteSpeed : 4f;
        }

        public Vector3[] GetWaypointPositions()
        {
            return WaypointPathSnapshot.CollectFromPathRoot(pathRoot);
        }

        public Vector3 GetStartPosition()
        {
            Vector3[] points = GetWaypointPositions();
            return points.Length > 0 ? points[0] : transform.position;
        }

        public Quaternion GetStartRotation()
        {
            Vector3[] points = GetWaypointPositions();

            if (points.Length < 2)
            {
                return transform.rotation;
            }

            Vector3 forward = points[1] - points[0];
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        public bool IsValid(out string error)
        {
            Vector3[] points = GetWaypointPositions();

            if (pathRoot == null)
            {
                error = "Не назначен Path Root.";
                return false;
            }

            if (points.Length < 2)
            {
                error = "Нужно минимум 2 точки под Path.";
                return false;
            }

            error = null;
            return true;
        }

        public bool HasEventsAtWaypoint(int waypointIndex)
        {
            for (int i = 0; i < waypointEvents.Length; i++)
            {
                if (waypointEvents[i] != null && waypointEvents[i].WaypointIndex == waypointIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void Reset()
        {
            if (pathRoot == null)
            {
                Transform path = transform.Find("Path");

                if (path != null)
                {
                    pathRoot = path;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawPathGizmo)
            {
                return;
            }

            Vector3[] points = GetWaypointPositions();

            if (points.Length == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.95f, 0.55f, 0.15f, 0.95f);

            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.color = HasEventsAtWaypoint(i)
                    ? new Color(1f, 0.2f, 0.85f, 0.95f)
                    : new Color(0.95f, 0.55f, 0.15f, 0.95f);
                Gizmos.DrawSphere(points[i], 0.12f);

                if (i < points.Length - 1)
                {
                    Gizmos.color = new Color(0.95f, 0.55f, 0.15f, 0.95f);
                    Gizmos.DrawLine(points[i], points[i + 1]);
                }
            }
        }
    }
}
