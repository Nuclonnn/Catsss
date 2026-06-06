#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Catsss.LevelKit.Editor
{
    [CustomEditor(typeof(KinematicPlatform))]
    public sealed class KinematicPlatformEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Path/Point_* — только для настройки. На spawn world-позиции кэшируются; root с collider двигается по кэшу.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            KinematicPlatform platform = (KinematicPlatform)target;
            Vector3[] points = platform.GetEditorWaypointPositions();

            if (points.Length == 0)
            {
                return;
            }

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);

            for (int i = 0; i < points.Length; i++)
            {
                float handleSize = HandleUtility.GetHandleSize(points[i]) * 0.1f;
                Handles.SphereHandleCap(0, points[i], Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(points[i] + Vector3.up * (handleSize * 2f), $"P{i}");

                if (i < points.Length - 1)
                {
                    Handles.DrawLine(points[i], points[i + 1]);
                }
            }
        }
    }
}
#endif
