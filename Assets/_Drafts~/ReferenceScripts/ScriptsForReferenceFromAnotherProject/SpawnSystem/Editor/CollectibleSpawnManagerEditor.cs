using UnityEditor;

[CustomEditor(typeof(CollectibleSpawnManager))]
public class CollectibleSpawnManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }
}
