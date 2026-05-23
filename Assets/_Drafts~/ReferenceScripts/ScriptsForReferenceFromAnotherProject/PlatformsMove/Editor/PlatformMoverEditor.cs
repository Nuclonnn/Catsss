using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlatformMover))]
public class PlatformMoverEditor : Editor
{
    SerializedProperty waypointsProperty;
    int selectedWaypointIndex = -1;

    void OnEnable() {
        waypointsProperty = serializedObject.FindProperty("waypoints");
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Кликни по точке в Scene. `W` двигает позицию waypoint, `E` редактирует его поворот.",
            MessageType.Info
        );

        if (selectedWaypointIndex >= 0) {
            EditorGUILayout.LabelField("Selected Waypoint", $"P{selectedWaypointIndex}");
        }
    }

    void OnSceneGUI() {
        var platformMover = (PlatformMover)target;
        serializedObject.Update();
        if (waypointsProperty == null || waypointsProperty.arraySize == 0) return;

        for (int i = 0; i < waypointsProperty.arraySize; i++) {
            Vector3 worldPoint = platformMover.GetWorldWaypointPosition(i);
            Quaternion worldRotation = platformMover.GetWorldWaypointRotation(i);

            bool isSelected = i == selectedWaypointIndex;
            Handles.color = isSelected ? Color.green : (i == 0 ? Color.yellow : Color.cyan);
            float handleSize = HandleUtility.GetHandleSize(worldPoint) * 0.12f;

            if (Handles.Button(worldPoint, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap)) {
                selectedWaypointIndex = i;
            }

            Handles.SphereHandleCap(0, worldPoint, Quaternion.identity, handleSize, EventType.Repaint);
            Handles.Label(worldPoint + Vector3.up * (handleSize * 2.5f), $"P{i}");
            Handles.ArrowHandleCap(0, worldPoint, worldRotation, handleSize * 4f, EventType.Repaint);
        }

        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < waypointsProperty.arraySize) {
            SerializedProperty selectedWaypointProperty = waypointsProperty.GetArrayElementAtIndex(selectedWaypointIndex);
            SerializedProperty positionOffsetProperty = selectedWaypointProperty.FindPropertyRelative("positionOffset");
            SerializedProperty rotationEulerProperty = selectedWaypointProperty.FindPropertyRelative("rotationEuler");
            Vector3 selectedWorldPoint = platformMover.GetWorldWaypointPosition(selectedWaypointIndex);
            Quaternion selectedWorldRotation = platformMover.GetWorldWaypointRotation(selectedWaypointIndex);

            DrawSelectedWaypointHandles(
                platformMover,
                positionOffsetProperty,
                rotationEulerProperty,
                selectedWorldPoint,
                selectedWorldRotation
            );
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(platformMover);
    }

    void DrawSelectedWaypointHandles(
        PlatformMover platformMover,
        SerializedProperty positionOffsetProperty,
        SerializedProperty rotationEulerProperty,
        Vector3 selectedWorldPoint,
        Quaternion selectedWorldRotation
    ) {
        Vector3 editorOriginPosition = platformMover.GetEditorOriginPosition();
        Quaternion editorOriginRotation = platformMover.GetEditorOriginRotation();

        if (Tools.current == Tool.Rotate) {
            EditorGUI.BeginChangeCheck();
            Quaternion rotatedWorldRotation = Handles.RotationHandle(selectedWorldRotation, selectedWorldPoint);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(platformMover, "Rotate Platform Waypoint");
                Quaternion rotationOffset = Quaternion.Inverse(editorOriginRotation) * rotatedWorldRotation;
                rotationEulerProperty.vector3Value = rotationOffset.eulerAngles;
            }

            return;
        }

        Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local
            ? selectedWorldRotation
            : Quaternion.identity;

        EditorGUI.BeginChangeCheck();
        Vector3 movedWorldPoint = Handles.PositionHandle(selectedWorldPoint, handleRotation);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(platformMover, "Move Platform Waypoint");
            positionOffsetProperty.vector3Value = movedWorldPoint - editorOriginPosition;
        }
    }
}
