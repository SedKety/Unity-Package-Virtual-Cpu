using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VirtualCPU;

/// <summary>
/// Partial — compile, run, and clean/normalise operations for <see cref="WhyEditorWindow"/>.
/// </summary>
public partial class WhyEditorWindow
{
    /// <summary>
    /// Normalises every hex value in the current content:
    /// pads single-digit hex to two digits (e.g. <c>0x5</c> → <c>0x05</c>) and
    /// uppercases the hex digits (e.g. <c>0x0a</c> → <c>0x0A</c>).
    /// The <c>0x</c> prefix is always kept lowercase.
    /// Pushes the current state onto the undo stack before modifying.
    /// </summary>
    private void CleanContent()
    {
        PushUndo();
        string raw = StripVisualPrefixes(_content);
        string[] lines = raw.Split('\n');
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                sb.Append('\n');
            sb.Append(NormalizeLineHex(lines[i].TrimEnd()));
        }
        _content = AddVisualPrefixes(sb.ToString());
        Repaint();
    }

    /// <summary>
    /// Compiles the current content and executes it in a <c>VCPU</c> instance,
    /// routing all output to <see cref="WhyTerminalWindow"/> via <see cref="WhyTerminalLogger"/>.
    /// Opens the terminal window automatically. VCPU diagnostic logging is disabled so only
    /// program output (from SysWrite etc.) appears in the terminal.
    /// </summary>
    private void RunScript()
    {
        byte[] program = CompileForEditor(StripVisualPrefixes(_content));

        var terminal = WhyTerminalWindow.GetOrOpen();
        terminal.Clear();

        if (program == null || program.Length == 0)
        {
            terminal.AppendError("Compile failed — no bytecode produced. Check the file for errors.");
            return;
        }

        terminal.Append($"▶  Compiled {program.Length} byte(s). Running...");

        var logger = new WhyTerminalLogger(terminal);
        try
        {
            new VCPU(program, logger, new HostCallLibrary[] { new UnityLib() }, false);
            terminal.Append("■  Done.");
        }
        catch (Exception ex)
        {
            terminal.AppendError($"Runtime exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses <paramref name="raw"/> through <see cref="ScriptCompiler"/> and extracts
    /// the bytes from the first <c>.Code</c> / <c>.HEX</c> sub-section.
    /// </summary>
    /// <param name="raw">Clean (prefix-stripped) <c>.why</c> source text.</param>
    /// <returns>The compiled byte array, or <c>null</c> if parsing throws.</returns>
    private static byte[] CompileForEditor(string raw)
    {
        try
        {
            var sections = ScriptCompiler.GetSections(ScriptCompiler.StripComments(raw));
            var program  = new List<byte>();
            foreach (var section in sections)
            {
                if (!section.Item1.Equals("Code", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var sub in section.Item2)
                {
                    if (sub.Item1 != Headers.HEX)
                        continue;
                    foreach (var line in sub.Item2)
                    {
                        for (int i = 0; i + 4 <= line.Length; i++)
                        {
                            if (line[i] != '0' || line[i + 1] != 'x')
                                continue;
                            string hex = line.Substring(i + 2, 2);
                            if (byte.TryParse(hex, NumberStyles.HexNumber, null, out byte b))
                                program.Add(b);
                            i += 3;
                        }
                    }
                }
            }
            return program.ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WhyEditor] Compile error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Normalises all hex values on a single line, leaving inline comments untouched.
    /// Single-digit hex is padded to two digits; hex digits are uppercased; the <c>0x</c> prefix stays lowercase.
    /// </summary>
    /// <param name="line">A single line of <c>.why</c> source (trailing whitespace already stripped).</param>
    private static string NormalizeLineHex(string line)
    {
        int ci = line.IndexOf(';');
        string code    = ci >= 0 ? line.Substring(0, ci) : line;
        string comment = ci >= 0 ? line.Substring(ci)    : string.Empty;

        //Pad single-digit hex: 0x5 → 0x05
        code = s_PadHex.Replace(code, m => "0x0" + m.Groups[1].Value.ToUpper());

        //Uppercase hex digits only (keep 'x' lowercase): 0x0a → 0x0A
        //Note tht i have no clue what this means, my regex knowledge is incredibly limited.
        code = Regex.Replace(code, @"(?<![0-9A-Fa-f])0x([0-9A-Fa-f]{2})(?![0-9A-Fa-f])", m => "0x" + m.Groups[1].Value.ToUpper());
        return code + comment;
    }
}
