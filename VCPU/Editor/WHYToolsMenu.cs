using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools/WHY menu editor utilities for WHY scripts and ScriptExecutionUnit components.
/// </summary>
public static class WHYToolsMenu
{
    /// <summary>
    /// Validates all ScriptExecutionUnit components in the currently loaded scenes 
    /// to ensure that their included script libraries match existing HostCallLibrary subclasses.
    /// </summary>
    [MenuItem("Tools/WHY/Validate Script Libraries")]
    private static void ValidateScriptLibraries()
    {
        int checked_ = 0, warnings = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            foreach (var seu in root.GetComponentsInChildren<ScriptExecutionUnit>(includeInactive: true))
            {
                checked_++;
                var script = GetScriptFile(seu);
                if (script == null)
                {
                    Debug.LogWarning($"[WHY] ({seu.name}) no script file assigned.", seu);
                    warnings++;
                    continue;
                }

                var includes = ScriptAssembler.ParseHeaders(script.text).Includes;
                foreach (var name in includes)
                {
                    bool found = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                        .Any(t => typeof(HostCallLibrary).IsAssignableFrom(t)
                               && !t.IsAbstract
                               && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (!found)
                    {
                        Debug.LogWarning($"[WHY] ({seu.name}) #include '{name}' does not match any HostCallLibrary subclass.", seu);
                        warnings++;
                    }
                    else
                    {
                        Debug.Log($"[WHY] ({seu.name}) #include '{name}' OK.", seu);
                    }
                }
            }
        }

        Debug.Log($"[WHY] Validate complete. {checked_} ScriptExecutionUnit(s) checked, {warnings} warning(s).");
        if (warnings > 0)
            EditorUtility.DisplayDialog("WHY Library Validation", $"{warnings} warning(s) found. See Console for details.", "OK");
        else
            EditorUtility.DisplayDialog("WHY Library Validation", $"All {checked_} unit(s) OK.", "OK");
    }

    private static TextAsset GetScriptFile(ScriptExecutionUnit seu)
    {
        var so = new SerializedObject(seu);
        return so.FindProperty("_scriptFile")?.objectReferenceValue as TextAsset;
    }
}
