#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Catsss.Gameplay.Mouse.Editor
{
    [CustomEditor(typeof(MouseRoute))]
    public sealed class MouseRouteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MouseRoute route = (MouseRoute)target;

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Path/Point_* — только для настройки. На Play сервер читает world-позиции детей Path.\n" +
                "Waypoint Events: назначь EmptyEventChannel и индекс Point_* (P0 = 0). " +
                "Тот же channel подключи к SignalDriver платформы/ветра/антимага.\n" +
                "ReturnHidden: мышь исчезает на последней точке маршрута (без телепорта в burrow).",
                MessageType.Info);

            if (!route.IsValid(out string error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Validate All Mouse Routes In Scene"))
            {
                ValidateAllRoutesInScene();
            }
        }

        private void OnSceneGUI()
        {
            MouseRoute route = (MouseRoute)target;
            Vector3[] points = route.GetWaypointPositions();

            if (points.Length == 0)
            {
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                bool hasEvent = route.HasEventsAtWaypoint(i);
                Handles.color = hasEvent
                    ? new Color(1f, 0.2f, 0.85f, 0.95f)
                    : new Color(0.95f, 0.55f, 0.15f, 0.95f);

                float handleSize = HandleUtility.GetHandleSize(points[i]) * 0.1f;
                Handles.SphereHandleCap(0, points[i], Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(
                    points[i] + Vector3.up * (handleSize * 2f),
                    hasEvent ? $"P{i} ⚡" : $"P{i}");

                if (i < points.Length - 1)
                {
                    Vector3 from = points[i];
                    Vector3 to = points[i + 1];
                    Vector3 direction = to - from;

                    Handles.color = new Color(0.95f, 0.55f, 0.15f, 0.95f);
                    Handles.DrawLine(from, to);

                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        Vector3 mid = Vector3.Lerp(from, to, 0.5f);
                        float arrowSize = handleSize * 1.4f;
                        Handles.ArrowHandleCap(
                            0,
                            mid,
                            Quaternion.LookRotation(direction.normalized, Vector3.up),
                            arrowSize,
                            EventType.Repaint);
                    }
                }
            }

            Vector3 endLabelPosition = points[^1] + Vector3.up * (HandleUtility.GetHandleSize(points[^1]) * 0.35f);
            Handles.Label(endLabelPosition, $"End: {route.EndMode}");
        }

        private static void ValidateAllRoutesInScene()
        {
            MouseRoute[] routes = Object.FindObjectsByType<MouseRoute>();
            int invalidCount = 0;

            foreach (MouseRoute route in routes)
            {
                if (route.IsValid(out string error))
                {
                    Debug.Log($"[MouseRoute] OK: '{route.name}'", route);
                }
                else
                {
                    invalidCount++;
                    Debug.LogWarning($"[MouseRoute] INVALID: '{route.name}' — {error}", route);
                }
            }

            if (routes.Length == 0)
            {
                Debug.Log("[MouseRoute] На сцене нет MouseRoute.");
            }
            else if (invalidCount == 0)
            {
                Debug.Log($"[MouseRoute] Все {routes.Length} маршрут(ов) валидны.");
            }
            else
            {
                Debug.LogWarning($"[MouseRoute] Невалидных маршрутов: {invalidCount} из {routes.Length}.");
            }
        }
    }
}
#endif
