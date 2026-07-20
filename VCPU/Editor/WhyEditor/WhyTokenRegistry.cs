using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Display metadata for a single opcode. Fields are editable directly in the Inspector.
/// </summary>
[Serializable]
public class WhyTokenEntry
{
    /// <summary>Normalised lowercase hex key, e.g. "0x05".</summary>
    public string hex;
    public Color color = Color.white;
    [TextArea(1, 2)] public string tooltip;
    public string[] argNames = new string[0];
}

/// <summary>
/// ScriptableObject registry mapping opcode hex values to editor display metadata.
/// Create via <b>Assets → Create → Why → Token Registry</b>, then right-click the asset
/// and choose <b>Populate Default Tokens</b> to pre-fill all built-in opcodes.
/// Add or edit entries directly in the Inspector — no code changes required.
/// </summary>
[CreateAssetMenu(fileName = "WhyTokenRegistry", menuName = "Why/Token Registry")]
public class WhyTokenRegistry : ScriptableObject
{
    public WhyTokenEntry[] tokens = new WhyTokenEntry[0];

    private static WhyTokenRegistry s_Instance;
    private Dictionary<string, WhyTokenEntry> _dict;

    /// <summary>
    /// Locates the first <see cref="WhyTokenRegistry"/> asset in the project, or <c>null</c> if none exists.
    /// </summary>
    public static WhyTokenRegistry Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;
            var guids = AssetDatabase.FindAssets("t:WhyTokenRegistry");
            if (guids.Length > 0)
                s_Instance = AssetDatabase.LoadAssetAtPath<WhyTokenRegistry>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            return s_Instance;
        }
    }

    /// <summary>
    /// Looks up an entry by normalised lowercase hex (e.g. <c>"0x05"</c>).
    /// Returns <c>true</c> if found.
    /// </summary>
    public bool TryGet(string normalizedHex, out WhyTokenEntry entry)
        => Dict.TryGetValue(normalizedHex, out entry);

    private Dictionary<string, WhyTokenEntry> Dict
    {
        get
        {
            if (_dict != null)
                return _dict;
            _dict = new Dictionary<string, WhyTokenEntry>();
            foreach (var t in tokens)
                if (!string.IsNullOrEmpty(t?.hex))
                    _dict[t.hex.ToLower()] = t;
            return _dict;
        }
    }

    private void OnValidate() => _dict = null;

    [ContextMenu("Populate Default Tokens")]
    private void PopulateDefaults()
    {
        tokens = new WhyTokenEntry[]
        {
            new WhyTokenEntry { hex="0x00", color=new Color(0.85f,0.30f,0.30f), tooltip="END — halt the CPU" },
            new WhyTokenEntry { hex="0x01", color=new Color(0.55f,0.55f,0.55f), tooltip="NOP — no operation" },
            new WhyTokenEntry { hex="0x02", color=new Color(0.35f,0.70f,1.00f), tooltip="CORECALL callIndex — invoke a built-in syscall",
                argNames=new[]{"call index"} },
            new WhyTokenEntry { hex="0x03", color=new Color(0.35f,0.70f,1.00f), tooltip="HOSTCALL libraryIndex functionIndex — invoke a host library function",
                argNames=new[]{"library index","function index"} },
            new WhyTokenEntry { hex="0x05", color=new Color(1.00f,0.80f,0.20f), tooltip="LOAD registerIndex value — load an immediate value into a register",
                argNames=new[]{"register","value"} },
            new WhyTokenEntry { hex="0x06", color=new Color(0.75f,0.40f,0.90f), tooltip="JMP address — unconditional jump",
                argNames=new[]{"address"} },
            new WhyTokenEntry { hex="0x07", color=new Color(0.40f,0.90f,0.55f), tooltip="MOV source destination — move between register and memory",
                argNames=new[]{"source","source is register","destination","destination is register"} },
            new WhyTokenEntry { hex="0x08", color=new Color(0.90f,0.55f,0.85f), tooltip="JNE — jump if not equal",  argNames=new[]{"address"} },
            new WhyTokenEntry { hex="0x09", color=new Color(0.90f,0.55f,0.85f), tooltip="JE  — jump if equal",      argNames=new[]{"address"} },
            new WhyTokenEntry { hex="0x0a", color=new Color(0.90f,0.55f,0.85f), tooltip="JL  — jump if less than",  argNames=new[]{"address"} },
            new WhyTokenEntry { hex="0x0b", color=new Color(0.90f,0.55f,0.85f), tooltip="JG  — jump if greater than", argNames=new[]{"address"} },
            new WhyTokenEntry { hex="0x14", color=new Color(1.00f,0.65f,0.20f), tooltip="ADD reg1 reg2 — add registers, result in reg1",
                argNames=new[]{"register 1 (result)","register 2"} },
            new WhyTokenEntry { hex="0x15", color=new Color(1.00f,0.65f,0.20f), tooltip="CMP reg1 reg2 — compare and set flags",
                argNames=new[]{"register 1","register 2"} },
            new WhyTokenEntry { hex="0x16", color=new Color(1.00f,0.65f,0.20f), tooltip="SUB reg1 reg2 — subtract reg2 from reg1",
                argNames=new[]{"register 1 (result)","register 2"} },
            new WhyTokenEntry { hex="0x17", color=new Color(1.00f,0.65f,0.20f), tooltip="INC register — increment register by 1",
                argNames=new[]{"register"} },
            new WhyTokenEntry { hex="0x18", color=new Color(1.00f,0.65f,0.20f), tooltip="DEC register — decrement register by 1",
                argNames=new[]{"register"} },
        };
        EditorUtility.SetDirty(this);
    }
}
