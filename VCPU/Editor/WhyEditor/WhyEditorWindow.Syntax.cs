using System;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Partial — syntax highlighting and hover tooltips for the Why Editor.
/// Converts the raw content string into Unity rich-text markup and resolves per-token
/// contextual descriptions based on the opcode's argument schema in <see cref="WhyTokenRegistry"/>.
/// </summary>
public partial class WhyEditorWindow
{
    private static readonly Color CommentColor     = new Color(0.50f, 0.70f, 0.50f);
    private static readonly Color HeaderColor      = new Color(0.70f, 0.50f, 1.00f);
    private static readonly Color PlainColor       = new Color(0.85f, 0.85f, 0.85f);
    private static readonly Color ErrorColor       = new Color(0.95f, 0.28f, 0.28f);

    // Each argument type gets its own hue so you can tell at a glance what role each byte plays.
    private static readonly Color ArgRegisterColor = new Color(0.45f, 0.90f, 0.90f);
    private static readonly Color ArgValueColor    = new Color(1.00f, 0.75f, 0.40f);
    private static readonly Color ArgAddressColor  = new Color(0.80f, 0.60f, 1.00f);
    private static readonly Color ArgFlagColor     = new Color(0.55f, 0.65f, 0.92f);
    private static readonly Color ArgIndexColor    = new Color(0.75f, 0.92f, 0.55f);
    private static readonly Color ArgDefaultColor  = new Color(0.72f, 0.72f, 0.72f);

    /// <summary>
    /// Converts all lines of <paramref name="content"/> into a Unity rich-text string
    /// suitable for rendering with a <c>richText = true</c> GUIStyle.
    /// </summary>
    private static string BuildRichText(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;
        var  reg          = WhyTokenRegistry.Instance;
        var  sb           = new StringBuilder(content.Length * 2);
        var  lines        = content.Split('\n');
        bool warnHeaders  = HeadersHasNoFormat(content);
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append('\n');
            bool isErrorHeader = warnHeaders && IsHeadersDirective(lines[i]);
            AppendLine(sb, lines[i], reg, isErrorHeader);
        }
        return sb.ToString();
    }

    private static bool IsHeadersDirective(string line)
    {
        string trimmed = line.TrimStart();
        return trimmed.StartsWith(".") &&
               SectionName(trimmed).Equals("headers", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true if a <c>.Headers</c> section exists in <paramref name="content"/>
    /// but contains no compilation-format declaration (HEX / ASM / DEC / BIN).
    /// </summary>
    internal static bool HeadersHasNoFormat(string content)
    {
        bool foundHeaders = false;
        bool inHeaders    = false;
        bool hasFormat    = false;
        foreach (var rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            int ci = line.IndexOf(';');
            if (ci >= 0)
                line = line.Substring(0, ci).Trim();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("."))
                continue;
            string name = line.Substring(1).Trim();
            if (name.Equals("headers", StringComparison.OrdinalIgnoreCase))
            {
                foundHeaders = true;
                inHeaders    = true;
                hasFormat    = false;
            }
            else if (inHeaders)
            {
                if (SubsectionNames.Contains(name))
                    hasFormat = true;
                else
                    inHeaders = false;
            }
        }
        return foundHeaders && !hasFormat;
    }

    /// <summary>
    /// Appends a single line of rich-text markup to <paramref name="sb"/>.
    /// Directive lines (starting with <c>.</c>) are coloured purple; inline comments green;
    /// hex tokens are coloured by their role (opcode colour, argument-type colour, or error red).
    /// Pass <paramref name="isErrorHeader"/> = true to colour a directive line red instead of purple.
    /// </summary>
    private static void AppendLine(StringBuilder sb, string line, WhyTokenRegistry reg, bool isErrorHeader = false)
    {
        string trimmed   = line.TrimStart();
        int    leading   = line.Length - trimmed.Length;
        if (trimmed.StartsWith("."))
        {
            Color headerCol = isErrorHeader ? ErrorColor : HeaderColor;
            if (leading > 0)
                AppendColoured(sb, line.Substring(0, leading), PlainColor);
            int hci = trimmed.IndexOf(';');
            if (hci >= 0)
            {
                AppendColoured(sb, trimmed.Substring(0, hci), headerCol);
                AppendColoured(sb, trimmed.Substring(hci), CommentColor);
            }
            else
            {
                AppendColoured(sb, trimmed, headerCol);
            }
            return;
        }

        int    ci      = line.IndexOf(';');
        string code    = ci >= 0 ? line.Substring(0, ci) : line;
        var    matches = Regex.Matches(code, @"0x[0-9A-Fa-f]+");

        int           opcodeIdx  = -1;
        WhyTokenEntry opcodeInfo = null;
        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].Value.Length != 4)
                continue;
            if (reg != null && reg.TryGet(NormalizeHex(matches[i].Value), out opcodeInfo))
            {
                opcodeIdx = i;
                break;
            }
        }

        if (opcodeIdx >= 0 && opcodeInfo?.argNames?.Length > 0 && matches.Count - opcodeIdx - 1 < opcodeInfo.argNames.Length)
        {
            AppendColoured(sb, code, ErrorColor);
            if (ci >= 0)
                AppendColoured(sb, line.Substring(ci), CommentColor);
            return;
        }

        int pos = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            if (m.Index > pos)
                AppendColoured(sb, code.Substring(pos, m.Index - pos), PlainColor);

            if (m.Value.Length > 10) // more than 8 hex digits overflows int
            {
                AppendColoured(sb, m.Value, ErrorColor);
                pos = m.Index + m.Length;
                continue;
            }

            string key = NormalizeHex(m.Value);
            if (i == opcodeIdx)
            {
                AppendColoured(sb, m.Value, opcodeInfo.color);
            }
            else if (i > opcodeIdx && opcodeIdx >= 0)
            {
                AppendColoured(sb, m.Value, GetArgColor(opcodeInfo, i - opcodeIdx - 1));
            }
            else if (reg != null && reg.TryGet(key, out var fi))
            {
                AppendColoured(sb, m.Value, fi.color);
            }
            else
            {
                AppendColoured(sb, m.Value, PlainColor);
            }

            pos = m.Index + m.Length;
        }
        if (pos < code.Length)
            AppendColoured(sb, code.Substring(pos), PlainColor);
        if (ci >= 0)
            AppendColoured(sb, line.Substring(ci), CommentColor);
    }

    /// <summary>
    /// Returns the highlight colour for the argument at <paramref name="argPos"/> of the given opcode.
    /// </summary>
    private static Color GetArgColor(WhyTokenEntry opcode, int argPos)
    {
        if (opcode?.argNames == null || argPos >= opcode.argNames.Length)
            return ArgDefaultColor;
        string name = opcode.argNames[argPos];
        if (name.Contains("is register"))
            return ArgFlagColor;
        if (name.Contains("register"))
            return ArgRegisterColor;
        if (name == "value")
            return ArgValueColor;
        if (name == "address")
            return ArgAddressColor;
        if (name.Contains("index"))
            return ArgIndexColor;
        if (name == "source")
            return ArgRegisterColor;
        if (name == "destination")
            return ArgAddressColor;
        return ArgDefaultColor;
    }

    private static void AppendColoured(StringBuilder sb, string text, Color c)
        => sb.Append($"<color=#{ToHex(c)}>{Escape(text)}</color>");

    private static string Escape(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string ToHex(Color c)
        => $"{B(c.r):X2}{B(c.g):X2}{B(c.b):X2}";

    private static byte B(float f) => (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255), 0, 255);

    /// <summary>
    /// Normalises a hex token to lowercase two-digit form for dictionary look-up
    /// (e.g. <c>"0x5"</c> → <c>"0x05"</c>, <c>"0x0A"</c> → <c>"0x0a"</c>).
    /// </summary>
    private static string NormalizeHex(string token)
    {
        if (!token.StartsWith("0x") || token.Length < 3)
            return token.ToLower();
        string hex = token.Substring(2).ToLower();
        return "0x" + (hex.Length == 1 ? "0" + hex : hex);
    }

    /// <summary>
    /// Converts a mouse position to a line/column index and determines which hex token (if any)
    /// is under the cursor, then populates <see cref="_hoveredTooltip"/> and <see cref="_tooltipScreenPos"/>.
    /// </summary>
    private void UpdateHover(Vector2 mouse)
    {
        _hoveredTooltip = null;
        if (!_editorRect.Contains(mouse))
            return;

        float relX  = (mouse.x - _editorRect.x) + _scroll.x - _editStyle.padding.left;
        float relY  = (mouse.y - _editorRect.y) + _scroll.y - _editStyle.padding.top;
        int lineIdx = Mathf.Max(0, Mathf.FloorToInt(relY / _lineHeight));
        int col     = Mathf.Max(0, Mathf.FloorToInt(relX / _charWidth));

        string[] lines = _content.Split('\n');
        if (lineIdx >= lines.Length)
            return;

        string line    = lines[lineIdx];
        int    ci      = line.IndexOf(';');
        string code    = ci >= 0 ? line.Substring(0, ci) : line;
        var    matches = Regex.Matches(code, @"0x[0-9A-Fa-f]+");

        int hoveredIdx = -1;
        for (int i = 0; i < matches.Count; i++)
        {
            if (col >= matches[i].Index && col < matches[i].Index + matches[i].Length)
            {
                hoveredIdx = i;
                break;
            }
        }
        if (hoveredIdx < 0)
            return;

        _tooltipScreenPos = mouse;

        if (matches[hoveredIdx].Value.Length > 10)
        {
            _hoveredTooltip = "Invalid hex — value overflows int (max 8 hex digits, e.g. 0xFFFFFFFF).";
            return;
        }

        string norm = NormalizeHex(matches[hoveredIdx].Value);
        var    reg  = WhyTokenRegistry.Instance;

        if (hoveredIdx == 0)
        {
            if (reg != null && reg.TryGet(norm, out var info))
                _hoveredTooltip = info.tooltip;
        }
        else
        {
            _hoveredTooltip = FindContextualTooltip(matches, hoveredIdx, norm, reg);
        }
    }

    /// <summary>
    /// Derives a contextual description for an argument token by looking up its position
    /// relative to the first (opcode) token on the line in <see cref="WhyTokenRegistry"/>.
    /// Always reads from <c>matches[0]</c> as the opcode — walking backward was unreliable
    /// because argument bytes can coincide with registered opcode values (e.g. 0x00 = END).
    /// </summary>
    private static string FindContextualTooltip(MatchCollection matches, int targetIdx, string targetNorm, WhyTokenRegistry reg)
    {
        if (matches.Count == 0 || reg == null)
            return null;
        if (!reg.TryGet(NormalizeHex(matches[0].Value), out var op))
            return null;
        int argPos = targetIdx - 1;
        if (op.argNames == null || argPos < 0 || argPos >= op.argNames.Length)
            return null;
        return FormatArgTooltip(op.argNames[argPos], targetNorm);
    }

    /// <summary>
    /// Formats a human-readable tooltip string for an argument byte given its role name and hex value.
    /// </summary>
    private static string FormatArgTooltip(string argName, string hexToken)
    {
        if (!hexToken.StartsWith("0x") || hexToken.Length < 3)
            return null;
        int val;
        try { val = System.Convert.ToInt32(hexToken.Substring(2), 16); }
        catch { return null; }

        switch (argName)
        {
            case "source is register":      return val == 1 ? "source: register" : "source: memory address";
            case "destination is register": return val == 1 ? "destination: register" : "destination: memory address";
            case "value":                   return $"value: {val}  ({hexToken})";
            case "address":                 return $"address: byte {val}  ({hexToken})";
            case "call index":
            case "library index":
            case "function index":          return $"{argName}: {val}";
            default:
                return argName.Contains("register") ? $"{argName}: r{val}" : $"{argName}: {val}  ({hexToken})";
        }
    }

    /// <summary>
    /// Draws the floating tooltip popup near the hovered token.
    /// The popup is positioned to stay within the window bounds and rendered with a solid
    /// dark background and a thin border.
    /// </summary>
    private void DrawTooltip()
    {
        if (string.IsNullOrEmpty(_hoveredTooltip))
            return;

        var   content = new GUIContent(_hoveredTooltip);
        float maxW    = Mathf.Min(position.width - 20f, 380f);
        float w       = Mathf.Min(_tooltipMeasureStyle.CalcSize(content).x, maxW);
        float h       = _tooltipTextStyle.CalcHeight(content, w);
        float tx      = Mathf.Clamp(_tooltipScreenPos.x + 14f, 4f, position.width  - w - 4f);
        float ty      = Mathf.Clamp(_tooltipScreenPos.y - h - 8f, 4f, position.height - h - 4f);
        var   tipRect = new Rect(tx, ty, w, h);

        EditorGUI.DrawRect(tipRect, new Color(0.10f, 0.10f, 0.14f));
        var border = new Color(0.35f, 0.35f, 0.48f);
        EditorGUI.DrawRect(new Rect(tipRect.x,         tipRect.y,         tipRect.width, 1f), border);
        EditorGUI.DrawRect(new Rect(tipRect.x,         tipRect.yMax - 1f, tipRect.width, 1f), border);
        EditorGUI.DrawRect(new Rect(tipRect.x,         tipRect.y,         1f, tipRect.height), border);
        EditorGUI.DrawRect(new Rect(tipRect.xMax - 1f, tipRect.y,         1f, tipRect.height), border);
        GUI.Label(tipRect, content, _tooltipTextStyle);
    }
}
