using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="ScriptExecutionUnit"/>.
/// Shows libraries declared via <c>#include</c> and editable overrides for every
/// <c>#define</c> declared in the assigned script.
/// All per-script data (includes, defines, library resolution) is cached and only
/// recomputed when the assigned script asset changes.
/// </summary>
[CustomEditor(typeof(ScriptExecutionUnit))]
public class ScriptExecutionUnitEditor : Editor
{
    private struct IncludeEntry
    {
        public string Name;
        public bool   Resolved;
        public int    LibraryId;
    }

    private TextAsset _lastScript;
    private Dictionary<string, string> _scriptDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int>    _overrideIndex  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private IncludeEntry[]             _includes        = Array.Empty<IncludeEntry>();

    private SerializedProperty _scriptFileProp;
    private SerializedProperty _overridesProp;

    private GUIStyle _dimStyle;

    private void OnEnable()
    {
        _scriptFileProp = serializedObject.FindProperty("_scriptFile");
        _overridesProp  = serializedObject.FindProperty("_defineOverrides");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.PropertyField(_scriptFileProp);

        var scriptFile = _scriptFileProp.objectReferenceValue as TextAsset;
        if (GUILayout.Button("Open Why Editor"))
            WhyEditorWindow.Open(scriptFile != null ? AssetDatabase.GetAssetPath(scriptFile) : null);

        if (scriptFile != _lastScript)
            RefreshCache(scriptFile);

        Separator();
        DrawIncludeSection(scriptFile);

        Separator();
        DrawDefineSection(scriptFile);

        Separator();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_scriptFile", "_defineOverrides");
        serializedObject.ApplyModifiedProperties();
    }

    private static void Separator()
    {
        EditorGUILayout.Space(6);
        Rect r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f, 1f));
        EditorGUILayout.Space(4);
    }

    private static GUIStyle s_subtitleStyle;

    private static void SectionHeader(string title, string subtitle = null)
    {
        Rect bg = EditorGUILayout.GetControlRect(false, 18f);
        EditorGUI.DrawRect(bg, new Color(0.18f, 0.18f, 0.22f, 1f));
        EditorGUI.LabelField(new Rect(bg.x + 6, bg.y + 1, bg.width - 6, bg.height), title, EditorStyles.boldLabel);
        if (subtitle != null)
        {
            if (s_subtitleStyle == null)
            {
                s_subtitleStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                s_subtitleStyle.normal.textColor = new Color(0.5f, 0.5f, 0.55f);
            }
            EditorGUI.LabelField(new Rect(bg.x, bg.y + 1, bg.width - 4, bg.height), subtitle, s_subtitleStyle);
        }
        EditorGUILayout.Space(2);
    }

    private void RefreshCache(TextAsset scriptFile)
    {
        _lastScript = scriptFile;
        _scriptDefaults.Clear();
        _overrideIndex.Clear();
        _includes = Array.Empty<IncludeEntry>();

        if (scriptFile == null) return;

        var headers = ScriptAssembler.ParseHeaders(scriptFile.text);
        _scriptDefaults = headers.Defines;

        _includes = headers.Includes.Select(name =>
        {
            var type = FindLibraryType(name);
            return new IncludeEntry { Name = name, Resolved = type != null, LibraryId = type != null ? GetLibraryId(type) : -1 };
        }).ToArray();

        SyncOverrideList();
    }

    private void DrawIncludeSection(TextAsset scriptFile)
    {
        SectionHeader("Libraries", "#include");

        if (scriptFile == null)   { EditorGUILayout.LabelField("  No script assigned.", EditorStyles.miniLabel); return; }
        if (_includes.Length == 0){ EditorGUILayout.LabelField("  No #include directives.", EditorStyles.miniLabel); return; }

        foreach (var e in _includes)
        {
            if (e.Resolved)
                EditorGUILayout.LabelField($"  ✓  {e.Name}  (ID 0x{e.LibraryId:X2})", EditorStyles.miniLabel);
            else
                EditorGUILayout.HelpBox($"#include '{e.Name}' — no HostCallLibrary subclass with that name exists.", MessageType.Warning);
        }
    }

    private void DrawDefineSection(TextAsset scriptFile)
    {
        SectionHeader("Defines", "#define  —  W writes to file, ↺ resets to script default");

        if (scriptFile == null)          { EditorGUILayout.LabelField("  No script assigned.", EditorStyles.miniLabel); return; }
        if (_scriptDefaults.Count == 0)  { EditorGUILayout.LabelField("  No #define directives.", EditorStyles.miniLabel); return; }

        if (_dimStyle == null)
        {
            _dimStyle = new GUIStyle(EditorStyles.miniLabel);
            _dimStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        }

        float labelW   = Mathf.Min(150f, EditorGUIUtility.currentViewWidth * 0.35f);
        float defaultW = 90f;

        foreach (var kv in _scriptDefaults)
        {
            if (!_overrideIndex.TryGetValue(kv.Key, out int idx)) continue;

            var valueProp = _overridesProp.GetArrayElementAtIndex(idx).FindPropertyRelative("Value");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(kv.Key, GUILayout.Width(labelW));
            EditorGUILayout.LabelField($"default: {kv.Value}", _dimStyle, GUILayout.Width(defaultW));
            valueProp.stringValue = EditorGUILayout.TextField(valueProp.stringValue, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("↺", EditorStyles.miniButton, GUILayout.Width(20)))
                valueProp.stringValue = kv.Value;
            if (GUILayout.Button("W", EditorStyles.miniButton, GUILayout.Width(20)))
                WriteDefineToScript(scriptFile, kv.Key, valueProp.stringValue);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void SyncOverrideList()
    {
        for (int i = _overridesProp.arraySize - 1; i >= 0; i--)
        {
            string n = _overridesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue;
            if (!_scriptDefaults.ContainsKey(n))
                _overridesProp.DeleteArrayElementAtIndex(i);
        }

        foreach (var kv in _scriptDefaults)
        {
            if (LinearFind(_overridesProp, kv.Key) < 0)
            {
                _overridesProp.InsertArrayElementAtIndex(_overridesProp.arraySize);
                var entry = _overridesProp.GetArrayElementAtIndex(_overridesProp.arraySize - 1);
                entry.FindPropertyRelative("Name").stringValue  = kv.Key;
                entry.FindPropertyRelative("Value").stringValue = kv.Value;
            }
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        _overrideIndex.Clear();
        for (int i = 0; i < _overridesProp.arraySize; i++)
            _overrideIndex[_overridesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue] = i;
    }

    private static int LinearFind(SerializedProperty list, string name)
    {
        for (int i = 0; i < list.arraySize; i++)
            if (string.Equals(list.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue,
                    name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private void WriteDefineToScript(TextAsset scriptFile, string name, string rawValue)
    {
        string value   = NormalizeValue(rawValue);
        string path    = AssetDatabase.GetAssetPath(scriptFile);
        string text    = File.ReadAllText(path);
        string pattern = $@"(?im)(#\s*define\s+{Regex.Escape(name)}\s+)[^\n;]+";
        string updated = Regex.Replace(text, pattern, m => m.Groups[1].Value + value);

        if (updated == text) return;

        File.WriteAllText(path, updated);
        AssetDatabase.ImportAsset(path);
        _lastScript = null; // force cache refresh on next repaint
    }

    private static string NormalizeValue(string raw)
    {
        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw)) return raw;

        bool hasF    = raw.EndsWith("f", StringComparison.OrdinalIgnoreCase);
        string numPart = hasF ? raw.Substring(0, raw.Length - 1) : raw;

        if (int.TryParse(numPart, out _))
            return numPart;

        if (float.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            return numPart + "f";

        return raw;
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
