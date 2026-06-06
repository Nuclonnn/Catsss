using UnityEditor;
using UnityEngine;

namespace Catsss.LevelKit.Editor
{
    [CustomEditor(typeof(KinematicPlatformSignalDriver))]
    public sealed class KinematicPlatformSignalDriverEditor : UnityEditor.Editor
    {
        private SerializedProperty _preset;
        private SerializedProperty _platform;
        private SerializedProperty _pressedChannel;
        private SerializedProperty _releasedChannel;
        private SerializedProperty _oneShotChannel;
        private SerializedProperty _onPressed;
        private SerializedProperty _onReleased;
        private SerializedProperty _onOneShot;
        private SerializedProperty _pressedTravelMode;
        private SerializedProperty _releasedTravelMode;
        private SerializedProperty _oneShotTravelMode;

        private void OnEnable()
        {
            _preset = serializedObject.FindProperty("preset");
            _platform = serializedObject.FindProperty("platform");
            _pressedChannel = serializedObject.FindProperty("pressedChannel");
            _releasedChannel = serializedObject.FindProperty("releasedChannel");
            _oneShotChannel = serializedObject.FindProperty("oneShotChannel");
            _onPressed = serializedObject.FindProperty("onPressed");
            _onReleased = serializedObject.FindProperty("onReleased");
            _onOneShot = serializedObject.FindProperty("onOneShot");
            _pressedTravelMode = serializedObject.FindProperty("pressedTravelMode");
            _releasedTravelMode = serializedObject.FindProperty("releasedTravelMode");
            _oneShotTravelMode = serializedObject.FindProperty("oneShotTravelMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_platform);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_preset);

            var presetValue = (KinematicPlatformSignalDriverPreset)_preset.enumValueIndex;

            if (presetValue != KinematicPlatformSignalDriverPreset.Custom)
            {
                EditorGUILayout.HelpBox(
                    "Пресет заполняет Actions/Travel ниже. Переключи на Custom для полностью ручной настройки.",
                    MessageType.Info);

                if (GUILayout.Button("Применить пресет"))
                {
                    ApplyPreset(presetValue);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pressedChannel);
            EditorGUILayout.PropertyField(_releasedChannel);
            EditorGUILayout.PropertyField(_oneShotChannel);
            DrawDuplicateChannelWarning();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onPressed);
            EditorGUILayout.PropertyField(_onReleased);
            EditorGUILayout.PropertyField(_onOneShot);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Travel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pressedTravelMode);
            EditorGUILayout.PropertyField(_releasedTravelMode);
            EditorGUILayout.PropertyField(_oneShotTravelMode);

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPreset(KinematicPlatformSignalDriverPreset presetValue)
        {
            var driver = (KinematicPlatformSignalDriver)target;
            Undo.RecordObject(driver, "Apply Signal Driver Preset");
            driver.ApplyPresetValues(presetValue);
            EditorUtility.SetDirty(driver);
            serializedObject.Update();
        }

        private void DrawDuplicateChannelWarning()
        {
            var pressed = _pressedChannel.objectReferenceValue;
            var released = _releasedChannel.objectReferenceValue;
            var oneShot = _oneShotChannel.objectReferenceValue;

            if (pressed == null)
            {
                return;
            }

            bool sharesReleased = released != null && pressed == released;
            bool sharesOneShot = oneShot != null && pressed == oneShot;
            bool releasedSharesOneShot = released != null && oneShot != null && released == oneShot;

            if (!sharesReleased && !sharesOneShot && !releasedSharesOneShot)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Один и тот же EmptyEventChannel на нескольких слотах: при каждом сигнале "
                + "сработают все подписанные Actions (Pressed + Released + OneShot). "
                + "Для Toggle используй отдельные каналы Pressed/Released и оставь One Shot пустым.",
                MessageType.Warning);
        }
    }
}
