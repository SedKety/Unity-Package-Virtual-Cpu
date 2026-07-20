using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Partial — file operations (load, save, new, open, discard, delete) and
/// path utility helpers for <see cref="WhyEditorWindow"/>.
/// </summary>
public partial class WhyEditorWindow
{
    /// <summary>
    /// Reads the file at <paramref name="path"/>, normalises its line endings,
    /// applies visual prefixes, and sets it as the current content.
    /// Also refreshes the Asset Database entry if the path is inside the project.
    /// </summary>
    private void LoadFile(string path)
    {
        _filePath = path;
        string raw = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n");
        _content = _savedContent = AddVisualPrefixes(StripVisualPrefixes(raw));
        _filePickerError = null;
        ClearHistory();
        string rel = ToRelative(path);
        if (rel != null)
            _pickedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(rel);
        Repaint();
    }

    /// <summary>
    /// Prompts for a save location, creates a minimal boilerplate <c>.why</c> file,
    /// and immediately saves it to disk.
    /// </summary>
    private void NewFile()
    {
        if (IsDirty && !ConfirmDiscard())
            return;
        string path = EditorUtility.SaveFilePanel("New Why File", Application.dataPath, "NewScript", "why");
        if (string.IsNullOrEmpty(path))
            return;
        _filePath        = path;
        _content         = AddVisualPrefixes(".Headers\n.HEX\n\n.Code\n0x00 ;Exit");
        _savedContent    = string.Empty;
        _pickedAsset     = null;
        _filePickerError = null;
        ClearHistory();
        SaveFile();
    }

    /// <summary>
    /// Opens a file-picker dialog and loads the selected <c>.why</c> file.
    /// </summary>
    private void OpenFile()
    {
        if (IsDirty && !ConfirmDiscard())
            return;
        string path = EditorUtility.OpenFilePanel("Open Why File", Application.dataPath, "why");
        if (!string.IsNullOrEmpty(path))
            LoadFile(path);
    }

    /// <summary>
    /// Writes the current content (with visual prefixes stripped) to <see cref="_filePath"/>,
    /// updating the saved-content baseline and refreshing the Asset Database.
    /// Prompts for a path if the file has not been saved before.
    /// </summary>
    private void SaveFile()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            string path = EditorUtility.SaveFilePanel("Save Why File", Application.dataPath, "NewScript", "why");
            if (string.IsNullOrEmpty(path))
                return;
            _filePath = path;
        }
        File.WriteAllText(_filePath, StripVisualPrefixes(_content));
        _savedContent = _content;
        string rel = ToRelative(_filePath);
        if (rel != null)
            AssetDatabase.ImportAsset(rel);
        Repaint();
    }

    /// <summary>
    /// Reloads the file from disk, discarding any unsaved changes after user confirmation.
    /// </summary>
    private void DiscardChanges()
    {
        if (!IsDirty || !ConfirmDiscard())
            return;
        string raw = !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath)
            ? File.ReadAllText(_filePath).Replace("\r\n", "\n").Replace("\r", "\n") : string.Empty;
        _content = _savedContent = AddVisualPrefixes(StripVisualPrefixes(raw));
        ClearHistory();
        Repaint();
    }

    /// <summary>
    /// Deletes the current file from disk (or the Asset Database if it is a project asset)
    /// after user confirmation, then resets the editor to an empty state.
    /// </summary>
    private void DeleteFile()
    {
        if (string.IsNullOrEmpty(_filePath))
            return;
        if (!EditorUtility.DisplayDialog("Delete", $"Delete '{Path.GetFileName(_filePath)}'?", "Delete", "Cancel"))
            return;
        string rel = ToRelative(_filePath);
        if (rel != null)
            AssetDatabase.DeleteAsset(rel);
        else
            File.Delete(_filePath);
        _filePath = null;
        _pickedAsset = null;
        _content = _savedContent = string.Empty;
        Repaint();
    }

    /// <summary>
    /// Shows the "Unsaved Changes — Discard?" dialog and returns the user's choice.
    /// </summary>
    private bool ConfirmDiscard()
        => EditorUtility.DisplayDialog("Unsaved Changes", "Discard unsaved changes?", "Discard", "Cancel");

    /// <summary>
    /// Converts an absolute path to a project-relative path starting with <c>Assets/</c>,
    /// or returns <c>null</c> if the path is outside the project.
    /// </summary>
    private static string ToRelative(string abs)
    {
        string data = Application.dataPath.Replace('\\', '/');
        abs = abs.Replace('\\', '/');
        return abs.StartsWith(data) ? "Assets" + abs.Substring(data.Length) : null;
    }

    /// <summary>
    /// Resolves <paramref name="path"/> to an absolute file-system path.
    /// Absolute paths are returned as-is (backslashes normalised); relative paths
    /// are resolved relative to the project root (one level above <c>Application.dataPath</c>).
    /// Returns <c>null</c> for null or empty input.
    /// </summary>
    private static string ResolveFullPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        if (Path.IsPathRooted(path))
            return path.Replace('\\', '/');
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)).Replace('\\', '/');
    }
}
