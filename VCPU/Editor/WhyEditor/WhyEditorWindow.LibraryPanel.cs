using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Partial — right-hand library reference panel.
/// Calls are grouped by the [Category] tag from UNITYINTEROP-SYNTAX.txt.
/// Drag the left edge to resize width; drag the top of the description box to resize it.
/// The [-]/[+] buttons scale panel text independently from the main editor zoom.
/// </summary>
public partial class WhyEditorWindow
{
    private const float PanelMinWidth    = 140f;
    private const float PanelMaxWidth    = 600f;
    private const float PanelDragW      = 6f;
    private const int   PanelMinFontSize = 8;
    private const int   PanelMaxFontSize = 28;

    private float _panelWidth          = 240f;
    private bool  _isDraggingPanel     = false;
    private bool  _isDraggingPanelDesc = false;
    private int   _panelFontSize       = 11;

    // Computed from _panelFontSize in EnsurePanelStyles; _panelDescH starts at -1 so
    // it gets initialised once from the font size, then owned by the drag handle.
    private float _panelHdrH    = 0f;
    private float _panelLibRowH = 0f;
    private float _panelRowH    = 0f;
    private float _panelCatH    = 0f;
    private float _panelDescH   = -1f;

    private bool    _showLibraryPanel  = false;
    private Vector2 _panelScroll;
    private string  _panelIncludesKey  = null;
    private string  _panelHoveredDesc  = null;
    private int     _panelLastFontSize = -1;

    private GUIStyle _panelIdStyle;
    private GUIStyle _panelNameStyle;
    private GUIStyle _panelLibNameStyle;
    private GUIStyle _panelCategoryStyle;
    private GUIStyle _panelDescStyle;
    private GUIStyle _panelPlaceholderStyle;

    private static readonly Color PanelBg         = new Color(0.12f, 0.12f, 0.15f);
    private static readonly Color PanelBorder      = new Color(0.28f, 0.28f, 0.40f);
    private static readonly Color PanelHeaderBg    = new Color(0.18f, 0.19f, 0.26f);
    private static readonly Color PanelCatBg       = new Color(0.14f, 0.15f, 0.20f);
    private static readonly Color PanelLibHeaderBg = new Color(0.20f, 0.22f, 0.30f);
    private static readonly Color PanelLibHoverBg  = new Color(0.24f, 0.26f, 0.38f);
    private static readonly Color PanelRowHoverBg  = new Color(0.20f, 0.22f, 0.33f);
    private static readonly Color PanelDescBg      = new Color(0.10f, 0.10f, 0.13f);
    private static readonly Color PanelDragHoverBg = new Color(0.40f, 0.44f, 0.60f);

    private struct LibraryEntry
    {
        public string Name;
        public int    ID;
        public List<(int callId, string callName, string category)> Calls;
    }

    private readonly List<LibraryEntry> _panelEntries = new List<LibraryEntry>();

    // ── Syntax description cache ─────────────────────────────────────────────

    private static Dictionary<string, string> s_syntaxDescs;

    private static Dictionary<string, string> GetSyntaxDescs()
    {
        if (s_syntaxDescs != null) return s_syntaxDescs;
        s_syntaxDescs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var guids = AssetDatabase.FindAssets("UNITYINTEROP-SYNTAX");
        if (guids.Length == 0) return s_syntaxDescs;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null) return s_syntaxDescs;

        string category = string.Empty;
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var m = Regex.Match(line, @"^\s+0x[0-9A-Fa-f]+\s+(\w+)(?:\s+(.+))?$");
            if (m.Success)
            {
                string name = m.Groups[1].Value;
                string desc = m.Groups[2].Success ? m.Groups[2].Value.Trim() : string.Empty;
                s_syntaxDescs[name] = string.IsNullOrEmpty(category) ? desc : $"[{category}]  {desc}";
            }
            else if (!line.StartsWith(" ") && !line.StartsWith("\t"))
            {
                string t = line.Trim();
                if (t.Length > 0 && !Regex.IsMatch(t, @"^=+$"))
                    category = t;
            }
        }
        return s_syntaxDescs;
    }

    private static string GetCallDescription(string callName) =>
        GetSyntaxDescs().TryGetValue(callName, out string d) ? d : null;

    private static string ExtractCategory(string desc)
    {
        if (string.IsNullOrEmpty(desc)) return string.Empty;
        var m = Regex.Match(desc, @"^\[([^\]]+)\]");
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    // ── Toggle ───────────────────────────────────────────────────────────────

    private void ToggleLibraryPanel()
    {
        _showLibraryPanel = !_showLibraryPanel;
        _panelHoveredDesc = null;
    }

    // ── Panel draw ───────────────────────────────────────────────────────────

    private void DrawLibraryPanel()
    {
        RefreshPanelIfNeeded();
        EnsurePanelStyles();

        Rect r = GUILayoutUtility.GetRect(_panelWidth, _panelWidth,
            GUILayout.Width(_panelWidth), GUILayout.ExpandHeight(true));

        var ev = Event.current;

        // ── Width drag handle (left edge) ────────────────────────────────────
        var widthDragRect = new Rect(r.x, r.y, PanelDragW, r.height);
        EditorGUIUtility.AddCursorRect(widthDragRect, MouseCursor.ResizeHorizontal);
        switch (ev.type)
        {
            case EventType.MouseDown when widthDragRect.Contains(ev.mousePosition):
                _isDraggingPanel = true; ev.Use(); break;
            case EventType.MouseDrag when _isDraggingPanel:
                _panelWidth = Mathf.Clamp(_panelWidth - ev.delta.x, PanelMinWidth, PanelMaxWidth);
                Repaint(); ev.Use(); break;
            case EventType.MouseUp when _isDraggingPanel:
                _isDraggingPanel = false; ev.Use(); break;
        }

        EditorGUI.DrawRect(r, PanelBg);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height),
            _isDraggingPanel ? PanelDragHoverBg : PanelBorder);

        // ── Header bar ───────────────────────────────────────────────────────
        var hdr = new Rect(r.x + 1f, r.y, r.width - 1f, _panelHdrH);
        EditorGUI.DrawRect(hdr, PanelHeaderBg);
        GUI.Label(new Rect(hdr.x + 8f, hdr.y + 2f, hdr.width - 80f, hdr.height),
            "Host Calls", EditorStyles.boldLabel);

        float btnW  = 18f;
        float numW  = 26f;
        float ctrlX = hdr.xMax - btnW - numW - btnW - 4f;
        float ctrlY = hdr.y + (hdr.height - btnW) * 0.5f;
        if (GUI.Button(new Rect(ctrlX, ctrlY, btnW, btnW), "-", EditorStyles.miniButton))
        {
            _panelFontSize = Mathf.Clamp(_panelFontSize - 1, PanelMinFontSize, PanelMaxFontSize);
            _panelIdStyle  = null;
            Repaint();
        }
        GUI.Label(new Rect(ctrlX + btnW, ctrlY, numW, btnW), _panelFontSize.ToString(),
            new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
        if (GUI.Button(new Rect(ctrlX + btnW + numW, ctrlY, btnW, btnW), "+", EditorStyles.miniButton))
        {
            _panelFontSize = Mathf.Clamp(_panelFontSize + 1, PanelMinFontSize, PanelMaxFontSize);
            _panelIdStyle  = null;
            Repaint();
        }

        // ── Description area ─────────────────────────────────────────────────
        float descMinH   = _panelFontSize * 2.5f + 10f;
        float descMaxH   = r.height - _panelHdrH - 40f;
        _panelDescH = Mathf.Clamp(_panelDescH, descMinH, Mathf.Max(descMinH, descMaxH));

        var descArea = new Rect(r.x + 1f, r.yMax - _panelDescH, r.width - 1f, _panelDescH);

        // Desc drag handle — top edge of the desc box
        var descDragRect = new Rect(descArea.x, descArea.y - 3f, descArea.width, 6f);
        EditorGUIUtility.AddCursorRect(descDragRect, MouseCursor.ResizeVertical);
        switch (ev.type)
        {
            case EventType.MouseDown when descDragRect.Contains(ev.mousePosition):
                _isDraggingPanelDesc = true; ev.Use(); break;
            case EventType.MouseDrag when _isDraggingPanelDesc:
                _panelDescH = Mathf.Clamp(_panelDescH - ev.delta.y, descMinH, Mathf.Max(descMinH, descMaxH));
                Repaint(); ev.Use(); break;
            case EventType.MouseUp when _isDraggingPanelDesc:
                _isDraggingPanelDesc = false; ev.Use(); break;
        }

        EditorGUI.DrawRect(descArea, PanelDescBg);
        EditorGUI.DrawRect(new Rect(descArea.x, descArea.y, descArea.width, 1f),
            _isDraggingPanelDesc ? PanelDragHoverBg : PanelBorder);
        var descInner = new Rect(descArea.x + 5f, descArea.y + 5f, descArea.width - 10f, _panelDescH - 10f);
        GUI.Label(descInner,
            string.IsNullOrEmpty(_panelHoveredDesc) ? "hover a call to see details" : _panelHoveredDesc,
            string.IsNullOrEmpty(_panelHoveredDesc) ? _panelPlaceholderStyle : _panelDescStyle);

        if (_panelEntries.Count == 0)
        {
            GUI.Label(new Rect(r.x + 8f, r.y + _panelHdrH + 6f, r.width - 12f, 40f),
                "No #include directives.", _panelPlaceholderStyle);
            return;
        }

        // ── Scrollable entries ────────────────────────────────────────────────
        float contentH   = ComputeContentHeight();
        var   scrollArea = new Rect(r.x + 1f, r.y + _panelHdrH, r.width - 1f,
                                    r.height - _panelHdrH - _panelDescH);

        _panelScroll = GUI.BeginScrollView(scrollArea, _panelScroll,
            new Rect(0, 0, scrollArea.width - 16f, Mathf.Max(contentH, scrollArea.height)));

        float  y       = 4f;
        float  w       = scrollArea.width - 20f;
        string newDesc = null;
        var    mouse   = ev.mousePosition;
        bool   isMove  = ev.type == EventType.MouseMove;
        bool   isClick = ev.type == EventType.MouseDown;

        foreach (var lib in _panelEntries)
        {
            // Library name row
            var libRect  = new Rect(0, y, w + 16f, _panelLibRowH);
            bool libHover = libRect.Contains(mouse);
            EditorGUI.DrawRect(libRect, libHover ? PanelLibHoverBg : PanelLibHeaderBg);
            if (libHover)
            {
                newDesc = lib.ID >= 0
                    ? $"Library: {lib.Name}\nID: 0x{lib.ID:X2}\nClick to insert name."
                    : $"{lib.Name} — unresolved library name.";
                if (isMove) Repaint();
            }
            if (isClick && libHover && lib.ID >= 0) { InsertAtCursor(lib.Name); ev.Use(); }

            string libLabel = lib.ID >= 0 ? $"{lib.Name}  (0x{lib.ID:X2})" : $"{lib.Name}  (?)";
            _panelLibNameStyle.normal.textColor = lib.ID >= 0 ? IncludeColor : ErrorColor;
            GUI.Label(new Rect(8f, y + 1f, w - 8f, _panelLibRowH - 2f), libLabel, _panelLibNameStyle);
            y += _panelLibRowH + 2f;

            if (lib.Calls.Count == 0)
            {
                GUI.Label(new Rect(14f, y, w - 14f, _panelRowH), "no calls found", _panelPlaceholderStyle);
                y += _panelRowH + 2f;
            }
            else
            {
                string lastCat = null;
                foreach (var (callId, callName, category) in lib.Calls)
                {
                    // Category sub-header when group changes
                    if (category != lastCat)
                    {
                        lastCat = category;
                        if (!string.IsNullOrEmpty(category))
                        {
                            EditorGUI.DrawRect(new Rect(0, y, w + 16f, _panelCatH), PanelCatBg);
                            GUI.Label(new Rect(8f, y, w - 8f, _panelCatH), category, _panelCategoryStyle);
                            y += _panelCatH;
                        }
                    }

                    var rowRect  = new Rect(0, y, w + 16f, _panelRowH);
                    bool rowHover = rowRect.Contains(mouse);
                    if (rowHover)
                    {
                        EditorGUI.DrawRect(rowRect, PanelRowHoverBg);
                        newDesc = GetCallDescription(callName) ?? callName;
                        if (isMove) Repaint();
                    }
                    if (isClick && rowHover) { InsertAtCursor(callName); ev.Use(); }

                    float idW = _panelFontSize * 3.2f;
                    GUI.Label(new Rect(14f,        y, idW,          _panelRowH), $"0x{callId:X2}", _panelIdStyle);
                    GUI.Label(new Rect(14f + idW,  y, w - idW - 8f, _panelRowH), callName,          _panelNameStyle);
                    y += _panelRowH;
                }
            }
            y += 6f;
        }

        if ((isMove || isClick) && newDesc != _panelHoveredDesc)
        { _panelHoveredDesc = newDesc; Repaint(); }
        else if (isMove && newDesc == null && _panelHoveredDesc != null)
        { _panelHoveredDesc = null; Repaint(); }

        GUI.EndScrollView();
    }

    // ── Data ────────────────────────────────────────────────────────────────

    private void RefreshPanelIfNeeded()
    {
        string key = BuildIncludesKey();
        if (key == _panelIncludesKey) return;
        _panelIncludesKey = key;
        _panelHoveredDesc = null;
        BuildPanelEntries();
    }

    private string BuildIncludesKey()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var raw in _content.Split('\n'))
        {
            string t = raw.TrimStart();
            if (t.StartsWith("#include", StringComparison.OrdinalIgnoreCase))
                sb.Append(t.ToLowerInvariant()).Append('\n');
        }
        return sb.ToString();
    }

    private void BuildPanelEntries()
    {
        _panelEntries.Clear();
        var stripped = StripVisualPrefixes(_content);
        var includes = ScriptAssembler.ParseHeaders(stripped).Includes;

        foreach (var name in includes)
        {
            var libType = ResolveLibraryType(name);
            if (libType == null)
            {
                _panelEntries.Add(new LibraryEntry
                {
                    Name = name, ID = -1,
                    Calls = new List<(int, string, string)>()
                });
                continue;
            }

            HostCallLibrary libInst;
            try { libInst = (HostCallLibrary)Activator.CreateInstance(libType); }
            catch { continue; }

            int libId = libInst.LibraryID;
            var calls = Assembly.GetAssembly(libType).GetTypes()
                .Where(t => typeof(IHostCall).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract
                         && t.GetCustomAttribute<HostCallLibraryAttribute>()?.LibraryID == libId)
                .Select(t => {
                    try
                    {
                        var c    = (IHostCall)Activator.CreateInstance(t);
                        string desc = GetCallDescription(t.Name);
                        string cat  = ExtractCategory(desc);
                        return (ok: true, id: c.ID, name: t.Name, cat: cat);
                    }
                    catch { return (ok: false, id: 0, name: t.Name, cat: string.Empty); }
                })
                .Where(x => x.ok)
                .OrderBy(x => string.IsNullOrEmpty(x.cat) ? "~" : x.cat) // uncategorised last
                .ThenBy(x => x.id)
                .Select(x => (x.id, x.name, x.cat))
                .ToList();

            _panelEntries.Add(new LibraryEntry { Name = name, ID = libId, Calls = calls });
        }
    }

    private static Type ResolveLibraryType(string typeName) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(t => typeof(HostCallLibrary).IsAssignableFrom(t)
                              && !t.IsAbstract
                              && t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

    private float ComputeContentHeight()
    {
        float h = 4f;
        foreach (var lib in _panelEntries)
        {
            h += _panelLibRowH + 2f;
            if (lib.Calls.Count == 0)
            {
                h += _panelRowH + 2f;
            }
            else
            {
                string lastCat = null;
                foreach (var (_, _, cat) in lib.Calls)
                {
                    if (cat != lastCat)
                    {
                        lastCat = cat;
                        if (!string.IsNullOrEmpty(cat)) h += _panelCatH;
                    }
                    h += _panelRowH;
                }
            }
            h += 6f;
        }
        return h;
    }

    // ── Styles (rebuild on panel font-size change) ────────────────────────────

    private void EnsurePanelStyles()
    {
        if (_panelIdStyle != null && _panelLastFontSize == _panelFontSize) return;
        _panelLastFontSize = _panelFontSize;

        _panelHdrH    = _panelFontSize + 10f;
        _panelLibRowH = _panelFontSize + 7f;
        _panelRowH    = _panelFontSize + 4f;
        _panelCatH    = _panelFontSize + 2f;

        // Only initialise _panelDescH on first build; preserve user-dragged size afterwards.
        if (_panelDescH < 0f)
            _panelDescH = Mathf.Max(56f, _panelFontSize * 4.5f);

        _panelIdStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = _panelFontSize };
        _panelIdStyle.normal.textColor = new Color(0.75f, 0.65f, 0.40f);

        _panelNameStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = _panelFontSize };
        _panelNameStyle.normal.textColor = new Color(0.82f, 0.82f, 0.82f);

        _panelLibNameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = _panelFontSize };

        _panelCategoryStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = _panelFontSize - 1 };
        _panelCategoryStyle.normal.textColor = new Color(0.55f, 0.60f, 0.75f);

        _panelDescStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = _panelFontSize, wordWrap = true };
        _panelDescStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f);

        _panelPlaceholderStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = _panelFontSize };
        _panelPlaceholderStyle.normal.textColor = new Color(0.42f, 0.42f, 0.45f);
    }
}
