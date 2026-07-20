using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom EditorWindow for authoring and running <c>.why</c> bytecode scripts.
/// Provides a syntax-highlighted code editor, file management, a clean/format pass,
/// and one-click execution via <see cref="WhyTerminalWindow"/>.
/// </summary>
/// <remarks>
/// The editor stores a <em>display</em> copy of the file in <see cref="_content"/> with visual
/// indent prefixes baked in; the on-disk file is always written with those prefixes stripped
/// (see <c>WhyEditorWindow.Editing.cs</c> — <c>AddVisualPrefixes</c> / <c>StripVisualPrefixes</c>).
///
/// The class is split across several partial files by concern:
/// <list type="bullet">
///   <item><c>WhyEditorWindow.cs</c>          — fields, lifecycle, OnGUI, toolbar, file-picker, zoom</item>
///   <item><c>WhyEditorWindow.Editing.cs</c>  — editor area, undo/redo, auto-formatting, indent, prefix helpers</item>
///   <item><c>WhyEditorWindow.Syntax.cs</c>   — syntax highlighting, hover tooltips</item>
///   <item><c>WhyEditorWindow.FileOps.cs</c>  — file load / save / delete, path utilities</item>
///   <item><c>WhyEditorWindow.Compile.cs</c>  — compile, run, clean/normalise</item>
/// </list>
/// </remarks>
public partial class WhyEditorWindow : EditorWindow
{
    // Mirrors the Headers enum values (minus NONE) so prefix helpers can identify
    // sub-section directives (.HEX, .ASM, .DEC, .BIN) without taking a dependency
    // on the ScriptCompiler assembly at parse time.

    /// <summary>
    /// Directive names that introduce a compilation-mode sub-section inside <c>.Code</c>.
    /// </summary>
    private static readonly HashSet<string> SubsectionNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HEX", "ASM", "DEC", "BIN" };

    /// <summary>
    /// Absolute path of the file currently open in the editor, or <c>null</c> for unsaved new files.
    /// </summary>
    private string _filePath;

    /// <summary>
    /// Display copy of the file content with visual indent prefixes applied.
    /// This is what the TextArea and the rich-text overlay both operate on.
    /// </summary>
    private string _content = string.Empty;

    /// <summary>
    /// The content as it was last saved to disk (with prefixes applied for comparison).
    /// </summary>
    private string _savedContent = string.Empty;

    private Vector2   _scroll;
    private TextAsset _pickedAsset;
    private string    _filePickerError;
    private GUIStyle  _editStyle;
    private GUIStyle  _displayStyle;
    private GUIStyle  _tooltipTextStyle;
    private GUIStyle  _tooltipMeasureStyle;
    private GUIStyle  _errorLabelStyle;
    private float     _charWidth;
    private float     _lineHeight;
    private Texture2D _clearTex;
    private string    _hoveredTooltip;
    private Vector2   _tooltipScreenPos;
    private Rect      _editorRect;

    private List<string> _undoStack = new List<string>();
    private List<string> _redoStack = new List<string>();

    private const int MaxUndoDepth = 200;

    /// <summary>
    /// Matches single-digit hex values (e.g. <c>0x0</c>, <c>0xA</c>) so <c>CleanContent</c>
    /// can pad them to two digits. Uses look-ahead/behind to avoid matching inside longer values.
    /// </summary>
    private static readonly Regex s_PadHex =
        new Regex(@"(?<![0-9A-Fa-f])0x([0-9A-Fa-f])(?![0-9A-Fa-f])", RegexOptions.Compiled);

    private int  _fontSize          = 13;
    private bool _stylesNeedRebuild = false;

    private const int    MinFontSize          = 8;
    private const int    MaxFontSize          = 36;
    private const int    DefaultFontSize      = 13;
    private const string TextAreaControlName  = "_whyed_";

    private bool   IsDirty      => _content != _savedContent;
    private string DisplayTitle =>
        (string.IsNullOrEmpty(_filePath) ? "Untitled" : Path.GetFileName(_filePath))
        + (IsDirty ? "*" : string.Empty);

    /// <summary>
    /// Opens (or focuses) the Why Editor window, optionally loading a file.
    /// If the window has unsaved changes the user is prompted to confirm before loading.
    /// </summary>
    /// <param name="filePath">Absolute or project-relative path to a <c>.why</c> file, or <c>null</c> to just open the window.</param>
    public static void Open(string filePath = null)
    {
        var w = GetWindow<WhyEditorWindow>("Why Editor");
        w.minSize = new Vector2(520, 420);
        string fullPath = ResolveFullPath(filePath);
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath) || fullPath == w._filePath)
            return;
        if (w.IsDirty && !EditorUtility.DisplayDialog("Unsaved Changes", "Discard unsaved changes?", "Discard", "Cancel"))
            return;
        w.LoadFile(fullPath);
    }

    private void OnEnable()  { wantsMouseMove = true; }

    private void OnDestroy()
    {
        if (_clearTex != null)
            DestroyImmediate(_clearTex);
    }

    /// <summary>
    /// Lazily builds all IMGUI styles. Called at the top of every <see cref="OnGUI"/> pass.
    /// Also rebuilds when <see cref="_stylesNeedRebuild"/> is set (e.g. after a zoom change).
    /// </summary>
    private void InitStyles()
    {
        if (_editStyle != null && !_stylesNeedRebuild)
            return;
        _stylesNeedRebuild = false;

        Font mono = Font.CreateDynamicFontFromOSFont(
            new[] { "Courier New", "Consolas", "Monaco", "Monospace" }, _fontSize);

        if (_clearTex == null)
        {
            _clearTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _clearTex.SetPixel(0, 0, Color.clear);
            _clearTex.Apply();
        }

        // Edit layer: invisible text so the rich-text display label shows through unobstructed.
        _editStyle = new GUIStyle(EditorStyles.textArea) { font = mono, fontSize = _fontSize, wordWrap = false, richText = false };
        SetAllStates(_editStyle, Color.clear, _clearTex);

        _displayStyle = new GUIStyle(_editStyle) { richText = true };
        SetTextColor(_displayStyle, new Color(0.85f, 0.85f, 0.85f));

        var measure = new GUIStyle(EditorStyles.textArea) { font = mono, fontSize = _fontSize, wordWrap = false };
        _charWidth  = measure.CalcSize(new GUIContent("WWWWWWWWWW")).x / 10f;
        _lineHeight = measure.CalcHeight(new GUIContent("A\nA"), 10000f) - measure.CalcHeight(new GUIContent("A"), 10000f);

        if (_charWidth  <= 0)
            _charWidth  = 8f;
        if (_lineHeight <= 0)
            _lineHeight = 16f;

        _tooltipTextStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, padding = new RectOffset(6, 6, 4, 4), fontSize = 12 };
        _tooltipTextStyle.normal.textColor = new Color(0.90f, 0.90f, 0.90f);
        _tooltipMeasureStyle = new GUIStyle(_tooltipTextStyle) { wordWrap = false };

        _errorLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        _errorLabelStyle.normal.textColor = new Color(1.00f, 0.50f, 0.30f);
    }

    static void SetAllStates(GUIStyle s, Color tc, Texture2D bg)
    {
        s.normal.textColor  = s.focused.textColor  = s.hover.textColor  = s.active.textColor  = tc;
        s.normal.background = s.focused.background = s.hover.background = s.active.background = bg;
        s.normal.scaledBackgrounds = s.focused.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
    }

    static void SetTextColor(GUIStyle s, Color tc)
        => s.normal.textColor = s.focused.textColor = s.hover.textColor = s.active.textColor = tc;

    private void OnGUI()
    {
        InitStyles();
        HandleZoom();
        HandleUndoRedo();
        titleContent = new GUIContent(DisplayTitle);
        DrawToolbar();
        DrawFilePicker();
        DrawWarnings();
        DrawEditor();
        DrawTooltip();
    }

    /// <summary>
    /// Draws a warning bar when the <c>.Headers</c> section exists but declares no compilation format.
    /// </summary>
    private void DrawWarnings()
    {
        if (string.IsNullOrEmpty(_content) || !HeadersHasNoFormat(_content))
            return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(".Headers has no format — add .HEX, .ASM, .DEC, or .BIN", _errorLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the top toolbar row with file-management and run buttons.
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("New",  EditorStyles.toolbarButton, GUILayout.Width(40)))
            NewFile();
        if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(44)))
            OpenFile();

        using (new EditorGUI.DisabledScope(!IsDirty))
        {
            if (GUILayout.Button("Save",    EditorStyles.toolbarButton, GUILayout.Width(40)))
                SaveFile();
            if (GUILayout.Button("Discard", EditorStyles.toolbarButton, GUILayout.Width(54)))
                DiscardChanges();
        }
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_filePath)))
        {
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(48)))
                DeleteFile();
        }

        if (GUILayout.Button("Clean", EditorStyles.toolbarButton, GUILayout.Width(48)))
            CleanContent();

        GUILayout.FlexibleSpace();
        GUILayout.Label(string.IsNullOrEmpty(_filePath) ? string.Empty : _filePath, EditorStyles.miniLabel);

        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_content)))
        {
            if (GUILayout.Button("Run", EditorStyles.toolbarButton, GUILayout.Width(36)))
                RunScript();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the second toolbar row with a TextAsset ObjectField for picking a <c>.why</c> file
    /// directly from the project. Validates that the selected asset has the correct extension.
    /// </summary>
    private void DrawFilePicker()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("File:", EditorStyles.miniLabel, GUILayout.Width(28));
        EditorGUI.BeginChangeCheck();
        var picked = (TextAsset)EditorGUILayout.ObjectField(_pickedAsset, typeof(TextAsset), false, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck() && picked != null)
        {
            string pickedPath = AssetDatabase.GetAssetPath(picked);
            if (!pickedPath.EndsWith(".why", StringComparison.OrdinalIgnoreCase))
            {
                _filePickerError = "Select a .why file";
            }
            else
            {
                _filePickerError = null;
                if (!IsDirty || ConfirmDiscard())
                {
                    _pickedAsset = picked;
                    string path = ResolveFullPath(pickedPath);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        LoadFile(path);
                }
            }
        }
        if (!string.IsNullOrEmpty(_filePickerError))
            GUILayout.Label(_filePickerError, _errorLabelStyle ?? EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Handles Ctrl+Scroll and Ctrl±/0 keyboard shortcuts to zoom the editor font in and out.
    /// </summary>
    private void HandleZoom()
    {
        Event e    = Event.current;
        bool  ctrl = e.control || e.command;
        if (!ctrl)
            return;

        if (e.type == EventType.ScrollWheel)
        {
            SetFontSize(_fontSize + (e.delta.y > 0 ? -1 : 1));
            e.Use();
            return;
        }

        if (e.type != EventType.KeyDown)
            return;

        if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus)
        {
            SetFontSize(_fontSize + 1);
            e.Use();
        }
        else if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
        {
            SetFontSize(_fontSize - 1);
            e.Use();
        }
        else if (e.keyCode == KeyCode.Alpha0 || e.keyCode == KeyCode.Keypad0)
        {
            SetFontSize(DefaultFontSize);
            e.Use();
        }
    }

    /// <summary>
    /// Clamps the font size and triggers a style rebuild if it changed.
    /// </summary>
    private void SetFontSize(int size)
    {
        int clamped = Mathf.Clamp(size, MinFontSize, MaxFontSize);
        if (clamped == _fontSize)
            return;
        _fontSize = clamped;
        _stylesNeedRebuild = true;
        Repaint();
    }
}
