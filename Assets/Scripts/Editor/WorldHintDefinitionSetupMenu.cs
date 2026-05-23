#if UNITY_EDITOR
using Catsss.Core.WorldHints;
using UnityEditor;
using UnityEngine;

namespace Catsss.EditorTools
{
    public static class WorldHintDefinitionSetupMenu
    {
        private const string DashHintPath = "Assets/Configs/WorldHints/Hint_PressShiftAfterTrial.asset";

        [MenuItem("Catsss/World Hints/Create Dash Hint Definition")]
        public static void CreateDashHintDefinition()
        {
            System.IO.Directory.CreateDirectory("Assets/Configs/WorldHints");

            var definition = ScriptableObject.CreateInstance<WorldTextHintDefinition>();
            AssetDatabase.CreateAsset(definition, DashHintPath);

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("text.editorFallback").stringValue = "Press Shift to dash";
            serialized.FindProperty("anchorMode").enumValueIndex = (int)WorldTextHintAnchorMode.LocalPlayerHead;
            serialized.FindProperty("localOffset").vector3Value = Vector3.zero;
            serialized.FindProperty("priority").intValue = 100;

            SerializedProperty showRule = serialized.FindProperty("showRule");
            showRule.FindPropertyRelative("kind").enumValueIndex = (int)WorldTextHintShowKind.PlayerCanDashUnlocked;
            showRule.FindPropertyRelative("requireLocalPlayerCanDash").boolValue = true;
            showRule.FindPropertyRelative("requireRuntimeEvent").boolValue = true;

            SerializedProperty hideRule = serialized.FindProperty("hideRule");
            hideRule.FindPropertyRelative("kind").enumValueIndex = (int)WorldTextHintHideKind.InputAction;
            hideRule.FindPropertyRelative("inputAction").enumValueIndex = (int)WorldTextHintInputKind.Dash;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            Selection.activeObject = definition;
            Debug.Log($"[Catsss] Created {DashHintPath}. Assign TrialProgressChannel and String Table keys.");
        }
    }
}
#endif
