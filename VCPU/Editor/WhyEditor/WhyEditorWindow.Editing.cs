using UnityEditor;
using UnityEngine;
using System;
using System.Text;

/// <summary>
/// Partial — editor area rendering, undo/redo, auto-formatting, Enter-key indent, and the
/// visual prefix helpers that keep the on-screen indent in sync with the on-disk content.
/// </summary>
public partial class WhyEditorWindow
{
    /// <summary>
    /// Renders the scrollable code editor.
    /// Uses a two-layer technique: a transparent <see cref="GUI.TextArea"/> on the bottom
    /// captures input and shows the cursor/selection, while a rich-text <see cref="GUI.Label"/>
    /// on top renders the syntax-highlighted output at exactly the same position and size.
    /// </summary>
    private void DrawEditor()
    {
        _editorRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(_editorRect, new Color(0.15f, 0.15f, 0.15f));

        float contentW = Mathf.Max(_editorRect.width, MaxLineWidth() * _charWidth + _editStyle.padding.horizontal + 80);
        float contentH = Mathf.Max(_editorRect.height, LineCount() * _lineHeight + _editStyle.padding.vertical + 20);

        GUI.BeginGroup(_editorRect);
        _scroll = GUI.BeginScrollView(
            new Rect(0, 0, _editorRect.width, _editorRect.height),
            _scroll,
            new Rect(0, 0, contentW, contentH));

        Rect textRect = new Rect(0, 0, contentW, contentH);

        // On Repaint, apply any deferred cursor BEFORE TextArea renders so the cursor
        // visual is correct even if the previous frame's te.cursorIndex didn't stick.
        if (_pendingCursor >= 0 && _textAreaControlID > 0 && Event.current.type == EventType.Repaint)
        {
            var preTe = GUIUtility.GetStateObject(typeof(TextEditor), _textAreaControlID) as TextEditor;
            if (preTe != null)
                preTe.cursorIndex = preTe.selectIndex = _pendingCursor;
            _pendingCursor = -1;
        }

        Color savedCursor = GUI.skin.settings.cursorColor;
        Color savedSelection = GUI.skin.settings.selectionColor;
        GUI.skin.settings.cursorColor = Color.white;
        GUI.skin.settings.selectionColor = new Color(0.25f, 0.50f, 0.90f, 0.40f);
        GUI.SetNextControlName(TextAreaControlName);
        string edited = GUI.TextArea(textRect, _content, _editStyle);
        GUI.skin.settings.cursorColor = savedCursor;
        GUI.skin.settings.selectionColor = savedSelection;

        // Capture the TextArea's control ID while we still can — GUIUtility.keyboardControl
        // may drop to 0 after Enter if Unity clears focus internally.
        {
            int kbCtrl = GUIUtility.keyboardControl;
            if (kbCtrl > 0)
                _textAreaControlID = kbCtrl;
        }

        if (edited.IndexOf('\r') >= 0)
            edited = edited.Replace("\r\n", "\n").Replace("\r", "\n");

        GUI.Label(textRect, BuildRichText(_content), _displayStyle);

        if (edited != _content)
        {
            _pendingCursor = -1;
            PushUndo();
            string formatted = ApplyAutoFormatting(_content, edited, out int newCursor);

            // Use stored control ID in case GUIUtility.keyboardControl is now 0.
            int ctrlID = _textAreaControlID > 0 ? _textAreaControlID : GUIUtility.keyboardControl;
            var te = GUIUtility.GetStateObject(typeof(TextEditor), ctrlID) as TextEditor;
            if (te != null)
            {
                te.text = formatted;
                if (newCursor >= 0)
                {
                    te.cursorIndex = te.selectIndex = newCursor;
                    _pendingCursor = newCursor;
                }
            }
            _content = formatted;
        }

        GUI.EndScrollView();
        GUI.EndGroup();

        EventType et = Event.current.type;
        if (et == EventType.MouseMove || et == EventType.Repaint)
            UpdateHover(Event.current.mousePosition);
        if (et == EventType.MouseMove)
            Repaint();
    }

    /// <summary>
    /// Intercepts the Enter key to inject a newline with the correct indent
    /// (matching the indent level of the current line's section context).
    /// </summary>
    private void HandleEnterForIndent()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter)
            return;
        if (GUI.GetNameOfFocusedControl() != TextAreaControlName)
            return;

        var te = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
        if (te == null)
            return;

        // Use _content (not te.text) as the authoritative source — te.text can lag when
        // ApplyAutoFormatting rewrites _content mid-frame, causing te.text != _content.
        int cursor = Mathf.Clamp(te.cursorIndex, 0, _content.Length);
        string indent = ComputeNextLineIndent(_content, cursor);
        if (indent.Length == 0)
            return;

        PushUndo();
        _content = _content.Substring(0, cursor) + "\n" + indent + _content.Substring(cursor);
        int newCursor = cursor + 1 + indent.Length;
        te.text = _content;
        te.cursorIndex = te.selectIndex = newCursor;
        e.Use();
    }

    /// <summary>
    /// Handles Ctrl+Z (undo) and Ctrl+Y / Ctrl+Shift+Z (redo) keyboard events.
    /// </summary>
    private void HandleUndoRedo()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown || (!e.control && !e.command))
            return;
        if (e.keyCode == KeyCode.Z && !e.shift)
        {
            PerformStep(_undoStack, _redoStack);
            e.Use();
        }
        else if (e.keyCode == KeyCode.Y || (e.keyCode == KeyCode.Z && e.shift))
        {
            PerformStep(_redoStack, _undoStack);
            e.Use();
        }
    }

    /// <summary>
    /// Saves the current content to the undo stack (skips if top is already identical).
    /// </summary>
    private void PushUndo()
    {
        if (_undoStack.Count > 0 && _undoStack[_undoStack.Count - 1] == _content)
            return;
        _undoStack.Add(_content);
        _redoStack.Clear();
        if (_undoStack.Count > MaxUndoDepth)
            _undoStack.RemoveAt(0);
    }

    private void PerformStep(System.Collections.Generic.List<string> from, System.Collections.Generic.List<string> to)
    {
        if (from.Count == 0)
            return;
        to.Add(_content);
        _content = from[from.Count - 1];
        from.RemoveAt(from.Count - 1);
        var te = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
        if (te != null)
        {
            te.text = _content;
            if (te.cursorIndex > _content.Length)
                te.cursorIndex = _content.Length;
            if (te.selectIndex > _content.Length)
                te.selectIndex = _content.Length;
        }
        Repaint();
    }

    private void ClearHistory() { _undoStack.Clear(); _redoStack.Clear(); }

    /// <summary>
    /// Called whenever the user types a character. Handles two cases:
    /// <list type="bullet">
    ///   <item>Enter — injects the correct indent on the new line.</item>
    ///   <item>. (dot) typed on an otherwise-blank line — snaps the cursor to the directive column.</item>
    /// </list>
    /// </summary>
    /// <param name="before">Content before the keystroke.</param>
    /// <param name="after">Content after the keystroke.</param>
    /// <param name="newCursorPos">Desired cursor position after formatting, or -1 to leave unchanged.</param>
    /// <returns>The formatted content string.</returns>
    private static string ApplyAutoFormatting(string before, string after, out int newCursorPos)
    {
        newCursorPos = -1;
        if (after.Length != before.Length + 1)
            return after;

        int insertPos = FindInsertPos(before, after);
        if (insertPos < 0)
            return after;
        char inserted = after[insertPos];

        if (inserted == '\n')
        {
            string indent = ComputeNextLineIndent(after, insertPos);
            if (indent.Length == 0)
                return after;
            newCursorPos = insertPos + 1 + indent.Length;
            return after.Substring(0, insertPos + 1) + indent + after.Substring(insertPos + 1);
        }

        if (inserted == '.')
        {
            int lineStart = insertPos > 0 ? after.LastIndexOf('\n', insertPos - 1) + 1 : 0;
            if (insertPos > lineStart)
            {
                bool allSpaces = true;
                for (int i = lineStart; i < insertPos; i++)
                    if (after[i] != ' ')
                    {
                        allSpaces = false;
                        break;
                    }

                if (allSpaces)
                {
                    // A directive sits one indent level above its content.
                    // ComputeIndent gives the content-level indent, so subtract 3.
                    int directiveLen = Math.Max(0, ComputeIndent(after.Substring(0, lineStart)).Length - 3);
                    int currentIndent = insertPos - lineStart;
                    if (currentIndent > directiveLen)
                    {
                        string prefix = directiveLen > 0 ? new string(' ', directiveLen) : string.Empty;
                        newCursorPos = lineStart + directiveLen + 1;
                        return after.Substring(0, lineStart) + prefix + after.Substring(insertPos);
                    }
                }
            }
        }

        return after;
    }

    /// <summary>
    /// Returns the index of the first character that differs between <paramref name="before"/> and <paramref name="after"/>.
    /// </summary>
    private static int FindInsertPos(string before, string after)
    {
        for (int i = 0; i < before.Length; i++)
            if (before[i] != after[i])
                return i;
        return before.Length;
    }

    /// <summary>
    /// Shared implementation for <see cref="AddVisualPrefixes"/> and <see cref="StripVisualPrefixes"/>.
    /// When <paramref name="add"/> is <c>true</c>, inserts visual indent spaces; otherwise strips them.
    /// </summary>
    private static string TransformPrefixes(string text, bool add)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        var lines = text.Split('\n');
        bool inSec = false, inHeaders = false, inSub = false;
        var sb = new StringBuilder(text.Length + (add ? lines.Length * 6 : 0));
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append('\n');
            string line = lines[i];
            string trimmed = line.TrimStart();
            bool isDir = trimmed.StartsWith(".");
            string name = isDir ? SectionName(trimmed) : null;
            bool isSub = name != null && SubsectionNames.Contains(name);

            int prefix = 0;
            if (!isDir && inSec)
                prefix = inSub ? 6 : 3;
            else if (isDir && inSec && !inHeaders && isSub)
                prefix = 3;

            if (add)
            {
                if (prefix > 0)
                    sb.Append(new string(' ', prefix));
                sb.Append(line);
            }
            else
            {
                int actual = 0;
                while (actual < line.Length && line[actual] == ' ')
                    actual++;
                int strip = Math.Min(prefix, actual);
                sb.Append(line, strip, line.Length - strip);
            }

            if (isDir)
            {
                if (inHeaders && isSub)
                {
                    // sub-section inside .Headers — skip state change
                }
                else if (inSec && !inHeaders && isSub)
                {
                    inSub = true;
                }
                else
                {
                    inSec = true;
                    inSub = false;
                    inHeaders = "headers".Equals(name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Prepends visual indent spaces to each line so the editor shows proper nesting
    /// without modifying the on-disk content.
    /// </summary>
    private static string AddVisualPrefixes(string content) => TransformPrefixes(content, true);

    /// <summary>
    /// Removes the visual indent spaces that <see cref="AddVisualPrefixes"/> added,
    /// recovering the clean on-disk format. Called before saving and before compiling.
    /// </summary>
    private static string StripVisualPrefixes(string display) => TransformPrefixes(display, false);

    /// <summary>
    /// Extracts the section/sub-section name from a directive line (text after the leading dot,
    /// stripping any inline comment and trimming whitespace).
    /// </summary>
    private static string SectionName(string trimmedLine)
    {
        string name = trimmedLine.Length > 1 ? trimmedLine.Substring(1) : "";
        int ci = name.IndexOf(';');
        if (ci >= 0)
            name = name.Substring(0, ci);
        return name.Trim();
    }

    /// <summary>
    /// Computes the indent string to place on the new line after pressing Enter at <paramref name="cursor"/>.
    /// For section/subsection directive lines (starting with <c>.</c>) the context-appropriate
    /// indent is derived from <see cref="ComputeIndent"/>; for all other lines the current
    /// line's leading spaces are matched.
    /// </summary>
    private static string ComputeNextLineIndent(string text, int cursor)
    {
        int lineStart = cursor > 0 ? (text.LastIndexOf('\n', cursor - 1) + 1) : 0;

        // Cursor is at the very start of a line (empty line or no text yet on this line).
        // Fall back to the previous line so pressing Enter on a blank/trailing line
        // still inherits the right indentation level.
        if (lineStart >= cursor)
        {
            if (lineStart == 0)
                return string.Empty;
            int prevEnd   = lineStart - 1;
            int prevStart = prevEnd > 0 ? (text.LastIndexOf('\n', prevEnd - 1) + 1) : 0;
            int prevWs    = prevStart;
            while (prevWs < prevEnd && prevWs < text.Length && text[prevWs] == ' ')
                prevWs++;
            return prevWs > prevStart ? text.Substring(prevStart, prevWs - prevStart) : string.Empty;
        }

        int ws = lineStart;
        while (ws < cursor && ws < text.Length && text[ws] == ' ')
            ws++;

        string lineContent = text.Substring(ws, Math.Max(0, cursor - ws)).TrimEnd();

        if (lineContent.StartsWith("."))
        {
            // After a directive, ask ComputeIndent what the section context demands.
            int lineEnd = text.IndexOf('\n', lineStart);
            int include = lineEnd >= 0 ? lineEnd : text.Length;
            return ComputeIndent(text.Substring(0, include));
        }

        return ws > lineStart ? text.Substring(lineStart, ws - lineStart) : string.Empty;
    }

    /// <summary>
    /// Returns the leading spaces of the line that contains position <paramref name="pos"/> in <paramref name="text"/>.
    /// Used to replicate the current line's indentation on a new line after Enter.
    /// </summary>
    private static string CurrentLineIndent(string text, int pos)
    {
        int lineStart = pos > 0 ? (text.LastIndexOf('\n', pos - 1) + 1) : 0;
        int ws = lineStart;
        while (ws < pos && ws < text.Length && text[ws] == ' ')
            ws++;
        return text.Substring(lineStart, ws - lineStart);
    }

    /// <summary>
    /// Determines the indent string that should be inserted after a newline at the given cursor
    /// position by scanning the preceding directives for the current section context.
    /// Returns <c>"      "</c> (6 spaces) inside a sub-section, <c>"   "</c> (3 spaces) inside a
    /// plain section, or an empty string before any section is opened.
    /// </summary>
    private static string ComputeIndent(string contentBefore)
    {
        bool inSection = false, inHeadersSection = false, inSubsection = false;
        foreach (string rawLine in contentBefore.Split('\n'))
        {
            string line = rawLine.TrimStart();
            if (!line.StartsWith("."))
                continue;
            string name = line.Substring(1);
            int ci = name.IndexOf(';');
            if (ci >= 0)
                name = name.Substring(0, ci);
            name = name.Trim();
            bool isSub = SubsectionNames.Contains(name);
            if (inHeadersSection && isSub)
                continue;
            if (inSection && !inHeadersSection && isSub)
            {
                inSubsection = true;
                continue;
            }
            inSection = true;
            inSubsection = false;
            inHeadersSection = name.Equals("headers", StringComparison.OrdinalIgnoreCase);
        }
        if (!inSection)
            return string.Empty;
        if (inSubsection)
            return "      ";
        return "   ";
    }

    /// <summary>
    /// Returns the number of lines in the current content (minimum 1).
    /// </summary>
    private int LineCount()
    {
        int n = 1;
        foreach (char c in _content)
            if (c == '\n')
                n++;
        return n;
    }

    /// <summary>
    /// Returns the length of the longest line in the current content (in characters).
    /// </summary>
    private int MaxLineWidth()
    {
        int max = 0, cur = 0;
        foreach (char c in _content)
        {
            if (c == '\n')
            {
                if (cur > max)
                    max = cur;
                cur = 0;
            }
            else
            {
                cur++;
            }
        }
        return cur > max ? cur : max;
    }
}
