using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Editor window that acts as the output terminal for VCPU script execution.
/// Displays timestamped log entries with colour-coded errors, and auto-scrolls to new output.
/// </summary>
/// <remarks>
/// Open via <see cref="GetOrOpen"/>. Entries are added with <see cref="Append"/> and
/// <see cref="AppendError"/>; the window is cleared automatically at the start of each run
/// by <see cref="WhyEditorWindow"/>.
/// </remarks>
public class WhyTerminalWindow : EditorWindow
{
    private static WhyTerminalWindow s_Instance;

    private readonly List<LogEntry> _entries = new List<LogEntry>();
    private Vector2 _scroll;
    private bool _autoScroll = true;
    private GUIStyle _logStyle;
    private GUIStyle _errStyle;
    private GUIStyle _tsStyle;
    private Action _rerunAction;
    private Action _stopAction;
    private bool _isRunning;

    [Serializable]
    private struct LogEntry { public string Text, Timestamp; public bool IsError; }

    /// <summary>
    /// Returns the existing terminal window, creating and focusing it if necessary.
    /// </summary>
    public static WhyTerminalWindow GetOrOpen()
    {
        if (s_Instance == null)
            s_Instance = GetWindow<WhyTerminalWindow>("Why Terminal");
        s_Instance.Focus();
        return s_Instance;
    }

    private void OnEnable() => s_Instance = this;

    public void SetRerunAction(Action a) => _rerunAction = a;
    public void SetStopAction(Action a)  => _stopAction  = a;
    public void SetRunning(bool running) { _isRunning = running; Repaint(); }

    /// <summary>
    /// Removes all entries and repaints the window.
    /// </summary>
    public void Clear() { _entries.Clear(); Repaint(); }

    /// <summary>
    /// Appends a normal (white) log entry and scrolls to it if auto-scroll is on.
    /// </summary>
    public void Append(string text)      => AddEntry(text, false);

    /// <summary>
    /// Appends a red error entry and scrolls to it if auto-scroll is on.
    /// </summary>
    public void AppendError(string text) => AddEntry(text, true);

    private void AddEntry(string text, bool isError)
    {
        _entries.Add(new LogEntry { Text = text, Timestamp = DateTime.Now.ToString("HH:mm:ss"), IsError = isError });
        if (_autoScroll)
            _scroll.y = float.MaxValue;
        Repaint();
    }

    private void InitStyles()
    {
        if (_logStyle != null)
            return;
        Font mono = Font.CreateDynamicFontFromOSFont(new[] { "Courier New", "Consolas", "Monaco", "Monospace" }, 12);
        _logStyle = new GUIStyle(EditorStyles.label) { font = mono, wordWrap = true, richText = false };
        _logStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        _errStyle = new GUIStyle(_logStyle);
        _errStyle.normal.textColor = new Color(1.00f, 0.40f, 0.30f);
        _tsStyle = new GUIStyle(EditorStyles.miniLabel);
        _tsStyle.normal.textColor = new Color(0.45f, 0.45f, 0.45f);
    }

    private void OnGUI()
    {
        InitStyles();
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44)))
            Clear();
        _autoScroll = GUILayout.Toggle(_autoScroll, "Auto-scroll", EditorStyles.toolbarButton, GUILayout.Width(78));
        using (new EditorGUI.DisabledScope(_isRunning || _rerunAction == null))
            if (GUILayout.Button("Re-run", EditorStyles.toolbarButton, GUILayout.Width(52)))
                _rerunAction?.Invoke();
        using (new EditorGUI.DisabledScope(!_isRunning))
            if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(40)))
                _stopAction?.Invoke();
        EditorGUILayout.EndHorizontal();

        EditorGUI.DrawRect(new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height), new Color(0.11f, 0.11f, 0.11f));
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var e in _entries)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(e.Timestamp, _tsStyle, GUILayout.Width(54));
            GUILayout.Label(e.Text, e.IsError ? _errStyle : _logStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
