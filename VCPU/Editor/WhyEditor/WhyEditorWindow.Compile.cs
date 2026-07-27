using UnityEngine;
using System;
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
    /// Compiles the current content and executes it in a <see cref="VCPU"/> instance,
    /// routing all output to <see cref="WhyTerminalWindow"/> via <see cref="WhyTerminalLogger"/>.
    /// Opens the terminal window automatically. <see cref="VCPU"/> diagnostic logging is disabled so only
    /// program output (from SysWrite etc.) appears in the terminal.
    /// </summary>
    private void RunScript()
    {
        AssemblyResult result = CompileForEditor(StripVisualPrefixes(_content));
        int[] program = result?.Program;

        var terminal = WhyTerminalWindow.GetOrOpen();
        terminal.Clear();

        if (program == null || program.Length == 0)
        {
            terminal.AppendError("Assemble failed; no bytecode produced. Check the file in the code editor for errors.");
            return;
        }

        //I am sorry to every developer that exists for slamming a "▶" character into the terminal output, but it is a very nice touch and i will defend that lols.
        //Also if someone reads this (doubt it, personal project)  try slam metal, shit's THE shit.
        terminal.Append($"▶  Compiled {program.Length} int(s). Running...");

        var headers = ScriptAssembler.ParseHeaders(StripVisualPrefixes(_content));
        var logger = new WhyTerminalLogger(terminal);
        try
        {
            var vcpu = new VCPU(program, logger, new VCPUSettings
            {
                Libraries = new HostCallLibrary[] { new UnityLib() },
                LoggingEnabled = false,
                MemorySize = headers.MemSize > 0 ? headers.MemSize : 16,
                StackSize = headers.StackSize > 0 ? headers.StackSize : 8,
                Entry = headers.Entry,
                Strict = headers.Strict,
                Timeout = headers.Timeout,
                NoHostCall = headers.NoHostCall,
                StackProtect = headers.StackProtect,
                DumpOnCrash = headers.DumpOnCrash,
                DumpOnExit = headers.DumpOnExit,
                Profile = headers.Profile,
                AutoRun = false,
            });

            int maxRuns = headers.Loops == 0 ? 1 : headers.Loops;
            bool forever = maxRuns == VCPUSettings.LoopForever;

            if (forever)
            {
                terminal.AppendError("LoopForever is not supported in the editor — running once.");
                maxRuns = 1;
                forever = false;
            }

            for (int run = 0; run < maxRuns; run++)
            {
                while (!vcpu.IsComplete)
                    vcpu.Step(int.MaxValue);

                if (run < maxRuns - 1)
                    vcpu.Restart();
            }

            //Yeah same here, nice touch but damn this sucks to look at in code.
            terminal.Append("■  Done.");
        }
        catch (Exception ex)
        {
            terminal.AppendError($"Runtime exception: {ex.Message}");
        }
    }

    /// <param name="raw">Clean (prefix-stripped) <c>.why</c> source text.</param>
    /// <returns>The compiled int array, or <c>null</c> if parsing throws.</returns>
    private static AssemblyResult CompileForEditor(string raw)
    {
        try
        {
            return ScriptAssembler.Assemble(raw);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WhyEditor] Assemble error: {ex.Message}");
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
        string code = ci >= 0 ? line.Substring(0, ci) : line;
        string comment = ci >= 0 ? line.Substring(ci) : string.Empty;

        //Pad single-digit hex: 0x5 -> 0x05
        code = s_PadHex.Replace(code, m => "0x0" + m.Groups[1].Value.ToUpper());

        //Uppercase hex digits only (keep 'x' lowercase): 0x0a → 0x0A
        //Note tht i have no clue what this means, my regex knowledge is incredibly limited. Study it if you read this later.
        code = Regex.Replace(code, @"(?<![0-9A-Fa-f])0x([0-9A-Fa-f]{2,8})(?![0-9A-Fa-f])", m => "0x" + m.Groups[1].Value.ToUpper());
        return code + comment;
    }
}
