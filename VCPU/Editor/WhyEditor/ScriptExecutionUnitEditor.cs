using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="ScriptExecutionUnit"/>.
/// Shows libraries declared via <c>#include</c> in the assigned script and warns
/// if any name does not resolve to a known <see cref="HostCallLibrary"/> subclass.
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
        if (GUILayout.Button("Open Why Editor"))
            WhyEditorWindow.Open(scriptFile != null ? AssetDatabase.GetAssetPath(scriptFile) : null);

        EditorGUILayout.Space();
        DrawIncludeSection(scriptFile);

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_scriptFile");
        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawIncludeSection(TextAsset scriptFile)
    {
        EditorGUILayout.LabelField("Libraries  (#include)", EditorStyles.boldLabel);

        if (scriptFile == null)
        {
            EditorGUILayout.LabelField("  No script assigned.", EditorStyles.miniLabel);
            return;
        }

        var includes = ScriptAssembler.ParseHeaders(scriptFile.text).Includes;
        if (includes.Length == 0)
        {
            EditorGUILayout.LabelField("  No #include directives.", EditorStyles.miniLabel);
            return;
        }

        foreach (var name in includes)
        {
            var resolved = FindLibraryType(name);
            if (resolved != null)
                EditorGUILayout.LabelField($"  ✓  {name}  (ID 0x{GetLibraryId(resolved):X2})", EditorStyles.miniLabel);
            else
                EditorGUILayout.HelpBox($"#include {name} — no HostCallLibrary subclass with that name exists.", MessageType.Warning);
        }
    }

    private static Type FindLibraryType(string typeName) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(t => typeof(HostCallLibrary).IsAssignableFrom(t)
                              && !t.IsAbstract
                              && t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

    private static int GetLibraryId(Type libraryType)
    {
        try { return ((HostCallLibrary)Activator.CreateInstance(libraryType)).LibraryID; }
        catch { return -1; }
    }
}
