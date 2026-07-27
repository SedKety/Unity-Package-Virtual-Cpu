using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Assembles any .why script into a runnable binary that can be executed by the VCPU.
/// </summary>
public static class ScriptAssembler
{
    #region Compiler Instances
    private static HexAssembler s_hexAssembler = new HexAssembler();
    private static AsmAssembler s_asmAssembler = new AsmAssembler();
    private static DecAssembler s_decAssembler = new DecAssembler();
    private static BinAssembler s_binAssembler = new BinAssembler();
    #endregion

    // Kept for backwards compatibility with ScriptExecutionUnit; no longer required.
    public static void Initialize(AssemblyTokenHolder tokenHolder) { }

    /// <summary>
    /// Takes a .why script and assembles it into a binary program that can be executed by the VCPU.
    /// </summary>
    public static AssemblyResult Assemble(TextAsset script) => Assemble(script.text);

    /// <summary>
    /// Assembles raw .why script text into a binary program that can be executed by the VCPU.
    /// </summary>
    public static AssemblyResult Assemble(string text)
    {
        var program = new List<int>();
        var globalLabels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var sections = GetSections(StripComments(text));

        foreach (var section in sections.Where(s => s.Item1.Equals("Code", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var subsection in section.Item2)
            {
                ITargetAssembler assembler = subsection.Item1 switch
                {
                    Headers.HEX  => s_hexAssembler,
                    Headers.ASM  => s_asmAssembler,
                    Headers.DEC  => s_decAssembler,
                    Headers.BIN  => s_binAssembler,
                    Headers.NONE => null,
                    _ => throw new Exception($"Unknown header type: {subsection.Item1}")
                };

                if (assembler == null)
                {
                    program.AddRange(CompileNone(subsection.Item2));
                    continue;
                }

                var (lines, localLabels) = ResolveLabels(subsection.Item2, assembler, program.Count);
                foreach (var kv in localLabels)
                    globalLabels[kv.Key] = kv.Value;
                program.AddRange(assembler.Assemble(lines));
            }
        }

        return new AssemblyResult(program.ToArray(), globalLabels);
    }

    /// <summary>
    /// Two-pass label resolver. Pass 1 maps every "label:" declaration to its absolute
    /// bytecode address (local address + <paramref name="globalOffset"/>) using
    /// <see cref="ITargetAssembler.CountTokens"/>. Pass 2 replaces every @label reference
    /// with the format-appropriate literal from <see cref="ITargetAssembler.FormatAddress"/>,
    /// leaving declaration lines in place so each assembler can skip them in its own loop.
    /// Returns the resolved lines and the label map (with global addresses) for this subsection.
    /// </summary>
    private static (string[] lines, Dictionary<string, int> labels) ResolveLabels(
        string[] rawLines, ITargetAssembler assembler, int globalOffset)
    {
        // Pass 1: collect labels, storing global addresses
        var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int address = 0;
        foreach (var rawLine in rawLines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (IsLabelLine(line))
                labels[line.TrimEnd(':').Trim()] = globalOffset + address;
            else
                address += assembler.CountTokens(line);
        }

        if (labels.Count == 0) return (rawLines, labels);

        // Pass 2: substitute @label references
        var result = new string[rawLines.Length];
        for (int i = 0; i < rawLines.Length; i++)
            result[i] = Regex.Replace(rawLines[i], @"@([A-Za-z_]\w*)", m =>
            {
                var name = m.Groups[1].Value;
                if (labels.TryGetValue(name, out int addr))
                    return assembler.FormatAddress(addr);
                Debug.LogWarning($"[ScriptAssembler] Label '@{name}' not found");
                return m.Value;
            });
        return (result, labels);
    }

    private static bool IsLabelLine(string line) => line.Length > 1 && line.TrimEnd().EndsWith(":");

    /// <summary>
    /// Replaced at compile-time by the method matching the standard header declared in .Headers.
    /// If no standard header is set, this section is ignored.
    /// </summary>
    private static int[] CompileNone(string[] lines) 
    { 
        Debug.LogError("[ScriptCompiler] No standard header declared in .Headers," +
            " ignoring this .Code section. " +
            "Can be resolved by: " +
            "\n 1. Declaring a standard header in .Headers." + 
            "\n 2. Manually declaring the header in the code section");
        return Array.Empty<int>(); 
    }

    /// <summary>
    /// Returns all the sections and sub-sections of the given file.
    /// </summary>
    /// <returns>Returns (sectionName, (headerType, lines[])[])[]</returns>
    public static Tuple<string, Tuple<Headers, string[]>[]>[] GetSections(string scriptText)
    {
        var sections = new List<Tuple<string, Tuple<Headers, string[]>[]>>();
        var lines = scriptText.Split('\n');

        string currentSection = null;
        bool inHeadersSection = false;
        var headersDeclarations = new List<string>();

        Headers defaultSubHeader = Headers.NONE;
        Headers currentSubHeader = Headers.NONE;
        var currentSubContent = new List<string>();
        var currentSubsections = new List<Tuple<Headers, string[]>>();

        void FlushSubsection()
        {
            currentSubsections.Add(Tuple.Create(currentSubHeader, currentSubContent.ToArray()));
            currentSubContent = new List<string>();
        }

        void FlushSection()
        {
            if (currentSection == null) return;

            if (inHeadersSection)
            {
                if (headersDeclarations.Count > 1)
                    throw new Exception($"Unexpected behavior: .headers defines multiple compilation types ({string.Join(", ", headersDeclarations)}). Only one is allowed.");

                if (headersDeclarations.Count == 1 && Enum.TryParse<Headers>(headersDeclarations[0], ignoreCase: true, out var declared))
                    defaultSubHeader = declared;

                sections.Add(Tuple.Create(currentSection,
                    new[] { Tuple.Create(Headers.NONE, headersDeclarations.ToArray()) }));
            }
            else
            {
                FlushSubsection();
                sections.Add(Tuple.Create(currentSection, currentSubsections.ToArray()));
            }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("."))
            {
                var name = line.Substring(1).Trim();

                if (currentSection != null && !inHeadersSection && Enum.TryParse<Headers>(name, ignoreCase: true, out var subHeader))
                {
                    FlushSubsection();
                    currentSubHeader = subHeader;
                    continue;
                }

                FlushSection();
                currentSection = name;
                inHeadersSection = name.Equals("headers", StringComparison.OrdinalIgnoreCase);
                headersDeclarations = new List<string>();
                currentSubHeader = inHeadersSection ? Headers.NONE : defaultSubHeader;
                currentSubContent = new List<string>();
                currentSubsections = new List<Tuple<Headers, string[]>>();
            }
            else if (line.StartsWith("#") && inHeadersSection)
            {
                var hashName = line.Substring(1).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (hashName.Length > 0 && Enum.TryParse<Headers>(hashName[0], ignoreCase: true, out _))
                    headersDeclarations.Add(hashName[0]);
            }
            else if (currentSection != null && !inHeadersSection)
            {
                currentSubContent.Add(line);
            }
        }

        FlushSection();

        return sections.ToArray();
    }

    public static string StripComments(string scriptText)
    {
        var lines = scriptText.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var j = lines[i].IndexOf(';');
            if (j >= 0) lines[i] = lines[i].Substring(0, j).Trim();
        }
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Parses the .Headers section of a .why script into a <see cref="ScriptHeaders"/> instance.
    /// Fields not present in the script are left at their defaults (0 / false).
    /// </summary>
    public static ScriptHeaders ParseHeaders(TextAsset script) => ParseHeaders(script.text);

    /// <summary>
    /// Parses the .Headers section of raw .why script text into a <see cref="ScriptHeaders"/> instance.
    /// Fields not present in the script are left at their defaults (0 / false).
    /// </summary>
    public static ScriptHeaders ParseHeaders(string scriptText)
    {
        var headers = new ScriptHeaders();
        var lines = StripComments(scriptText).Split('\n');
        bool inHeaders = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("."))
            {
                var sectionName = line.Substring(1).Trim().ToUpperInvariant();
                inHeaders = sectionName == "HEADERS";
                continue;
            }

            if (!inHeaders || !line.StartsWith("#")) continue;

            var directive = line.Substring(1).Trim();
            var parts = directive.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var key = parts[0].ToUpperInvariant();
            var value = parts.Length > 1 ? parts[1].Trim() : "";

            switch (key)
            {
                case "HEX": headers.Format = Headers.HEX; break;
                case "ASM": headers.Format = Headers.ASM; break;
                case "DEC": headers.Format = Headers.DEC; break;
                case "BIN": headers.Format = Headers.BIN; break;
                case "MEMSIZE": headers.MemSize = (uint)ParseHexOrDec(value); break;
                case "STACKSIZE": headers.StackSize = (uint)ParseHexOrDec(value); break;
                case "ENTRY":
                    if (!string.IsNullOrWhiteSpace(value) && char.IsLetter(value[0]))
                        headers.EntryLabel = value;
                    else
                        headers.Entry = ParseHexOrDec(value);
                    break;
                case "TIMEOUT": headers.Timeout = ParseHexOrDec(value); break;
                case "TICK_RATE": headers.TickRate = ParseHexOrDec(value); break;
                case "LOOP": headers.Loops = ParseHexOrDec(value); break;
                case "DEBUG": headers.Debug = true; break;
                case "STRICT": headers.Strict = true; break;
                case "DUMP_ON_CRASH": headers.DumpOnCrash = true; break;
                case "NO_HOSTCALL": headers.NoHostCall = true; break;
                case "PROFILE": headers.Profile = true; break;
                case "DUMP_ON_EXIT": headers.DumpOnExit = true; break;
                case "STACK_PROTECT": headers.StackProtect = true; break;
            }
        }

        if (headers.EntryLabel != null)
            headers.Entry = ResolveEntryLabel(scriptText, headers.EntryLabel);

        return headers;
    }

    /// <summary>
    /// Resolves a label name to its global bytecode address by scanning all code sections.
    /// Throws if the label is not defined in any ASM section.
    /// </summary>
    public static int ResolveEntryLabel(string scriptText, string labelName)
    {
        var sections = GetSections(StripComments(scriptText));
        int globalOffset = 0;

        foreach (var section in sections)
        {
            if (!section.Item1.Equals("Code", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var subsection in section.Item2)
            {
                if (subsection.Item1 == Headers.ASM)
                {
                    var labels = s_asmAssembler.BuildLabelMap(subsection.Item2);
                    if (labels.TryGetValue(labelName, out int localAddr))
                        return globalOffset + localAddr;
                    globalOffset += s_asmAssembler.Assemble(subsection.Item2).Length;
                }
                else
                {
                    int[] compiled = subsection.Item1 switch
                    {
                        Headers.HEX  => s_hexAssembler.Assemble(subsection.Item2),
                        Headers.DEC  => s_decAssembler.Assemble(subsection.Item2),
                        Headers.BIN  => s_binAssembler.Assemble(subsection.Item2),
                        _            => Array.Empty<int>(),
                    };
                    globalOffset += compiled.Length;
                }
            }
        }

        throw new Exception($"[ScriptAssembler] Entry label '{labelName}' is not defined in any ASM section.");
    }

    private static int ParseHexOrDec(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;

        if (value.StartsWith("0x") || value.StartsWith("0X"))
        {
            if (int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out int hex))
                return hex;
        }

        if (int.TryParse(value, out int dec))
            return dec;

        Debug.LogWarning($"[ScriptCompiler] Could not parse header value '{value}', defaulting to 0");
        return 0;
    }
}

/// <summary>
/// Defines the possible headers for the script compiler, which determine how the code section is interpreted.
/// </summary>
public enum Headers
{
    /// <summary>
    /// Zero headers
    /// </summary>
    NONE,

    /// <summary>
    /// Read the code-section as Assembly code
    /// </summary>
    ASM,

    /// <summary>
    /// Read the code-section as opcodes written in hexadecimal (0x00, 0x0A, 0x64)
    /// </summary>
    HEX,

    /// <summary>
    /// Read the code-section as opcodes written in decimal (0, 10, 100)
    /// </summary>
    DEC,

    /// <summary>
    /// Reads the code-section as binary, please dont fucking do this (0000, 1010, 1100100) 
    /// </summary>
    /// <remarks>Apologies for the cursing but this is really not recommended. </remarks>
    BIN
}
