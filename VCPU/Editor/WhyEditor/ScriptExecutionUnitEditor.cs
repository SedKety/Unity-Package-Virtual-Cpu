using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="ScriptExecutionUnit"/>.
/// Draws the script-file picker and an "Open Why Editor" button that launches
/// <see cref="WhyEditorWindow"/> with the currently assigned <c>.why</c> asset.
/// </summary>
[CustomEditor(typeof(ScriptExecutionUnit))]
public class ScriptExecutionUnitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        var scriptFileProp = serializedObject.FindProperty("_scriptFile");
        EditorGUILayout.PropertyField(scriptFileProp);

        var scriptFile = scriptFileProp.objectReferenceValue as TextAsset;
        string assetPath = scriptFile != null ? AssetDatabase.GetAssetPath(scriptFile) : null;

        if (GUILayout.Button("Open Why Editor"))
            WhyEditorWindow.Open(assetPath);

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_scriptFile");
        serializedObject.ApplyModifiedProperties();
    }
}
