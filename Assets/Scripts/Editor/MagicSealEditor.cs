using UnityEditor;
using UnityEngine;

namespace Catsss.LevelKit.Editor
{
    [CustomEditor(typeof(MagicSeal))]
    public sealed class MagicSealEditor : UnityEditor.Editor
    {
        private SerializedProperty _activationPolicy;
        private SerializedProperty _interactPulseSeconds;
        private SerializedProperty _startDisabled;
        private SerializedProperty _enableDebugLogs;
        private SerializedProperty _pressedChannel;
        private SerializedProperty _releasedChannel;
        private SerializedProperty _oneShotChannel;

        private void OnEnable()
        {
            _activationPolicy = serializedObject.FindProperty("activationPolicy");
            _interactPulseSeconds = serializedObject.FindProperty("interactPulseSeconds");
            _startDisabled = serializedObject.FindProperty("startDisabled");
            _enableDebugLogs = serializedObject.FindProperty("enableDebugLogs");
            _pressedChannel = serializedObject.FindProperty("pressedChannel");
            _releasedChannel = serializedObject.FindProperty("releasedChannel");
            _oneShotChannel = serializedObject.FindProperty("oneShotChannel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_activationPolicy);
            EditorGUILayout.PropertyField(_startDisabled);
            EditorGUILayout.PropertyField(_enableDebugLogs);

            MagicSealActivationPolicy policy = (MagicSealActivationPolicy)_activationPolicy.enumValueIndex;

            if (policy == MagicSealActivationPolicy.Momentary)
            {
                EditorGUILayout.PropertyField(_interactPulseSeconds);
                EditorGUILayout.HelpBox(
                    "Momentary через E — короткий импульс (Pressed → Released). Для удержания используй trigger-плиту.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SO Event Channels", EditorStyles.boldLabel);

            switch (policy)
            {
                case MagicSealActivationPolicy.OneShot:
                    EditorGUILayout.PropertyField(_oneShotChannel);
                    EditorGUILayout.HelpBox("OneShot шлёт только OneShot Channel (без Pressed/Released).", MessageType.None);
                    break;
                case MagicSealActivationPolicy.Toggle:
                    EditorGUILayout.PropertyField(_pressedChannel);
                    EditorGUILayout.PropertyField(_releasedChannel);
                    EditorGUILayout.HelpBox("Toggle: первое нажатие → Pressed, второе → Released.", MessageType.None);
                    break;
                default:
                    EditorGUILayout.PropertyField(_pressedChannel);
                    EditorGUILayout.PropertyField(_releasedChannel);
                    EditorGUILayout.HelpBox(
                        "Trigger-Momentary: Pressed пока игрок в зоне, Released при выходе.",
                        MessageType.None);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
