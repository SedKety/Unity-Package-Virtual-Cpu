using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

    public static AssemblyResult Assemble(TextAsset script) => Assemble(script.text, null);
    public static AssemblyResult Assemble(TextAsset script, Dictionary<string, string> extraDefines)
        => Assemble(script.text, extraDefines);
    public static AssemblyResult Assemble(string text) => Assemble(text, null);

    /// <summary>
    /// Assembles raw .why script text into a runnable integer program.
    /// All executable code lives in .Code. Subroutines go after an END instruction
    /// in .Code CALL jumps to them, RET jumps back; they never auto-execute.
    /// Labels are resolved in a global two-pass so forward references work freely.
    /// <paramref name="extraDefines"/> are merged after parsing they override any same-named #define in the script.
    /// </summary>
    public static AssemblyResult Assemble(string text, Dictionary<string, string> extraDefines)
    {
        var headers = ParseHeaders(text);
        if (extraDefines != null)
            foreach (var kv in extraDefines)
                headers.Defines[kv.Key] = kv.Value;
        InjectLibraryDefines(headers);
        var stripped = StripComments(text);
        var sections = GetSections(
            headers.Defines.Count > 0 ? ApplyDefines(stripped, headers.Defines) : stripped);
        var globalLabels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int offset = 0;
        CollectSectionLabels(sections, "Code", ref offset, globalLabels);

        var program = new List<int>();
        AssembleSection(sections, "Code", program, globalLabels);

        return new AssemblyResult(program.ToArray(), globalLabels);
    }

    /// <summary>
    /// Collects all labels from the given section and its sub-sections,
    /// storing them in the global label map with their absolute addresses.
    /// </summary>
    /// <param name="sections">The sections to collect labels from.</param>
    /// <param name="sectionName">The name of the section to collect labels from.</param>
    /// <param name="offset">The current offset in the program.</param>
    /// <param name="globalLabels">The global label map to store labels in.</param>
    private static void CollectSectionLabels(
        Tuple<string, Tuple<Headers, string[]>[]>[] sections,
        string sectionName,
        ref int offset,
        Dictionary<string, int> globalLabels)
    {
        foreach (var section in sections.Where(s =>
            s.Item1.Equals(sectionName, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var subsection in section.Item2)
            {
                var assembler = GetAssembler(subsection.Item1);
                if (assembler == null) continue;

                foreach (var rawLine in subsection.Item2)
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (IsLabelLine(line))
                        globalLabels[line.TrimEnd(':').Trim()] = offset;
                    else
                        offset += assembler.CountTokens(line);
                }
            }
        }
    }

    /// <summary>
    /// Assembles the given section and its sub-sections into the program list,
    /// </summary>
    /// <param name="sections">The sections to assemble.</param>
    /// <param name="sectionName">The name of the section to assemble.</param>
    /// <param name="program">The program list to append the assembled code to.</param>
    /// <param name="globalLabels">The global label map for resolving label references.</param>
    private static void AssembleSection(
        Tuple<string, Tuple<Headers, string[]>[]>[] sections,
        string sectionName,
        List<int> program,
        IReadOnlyDictionary<string, int> globalLabels)
    {
        foreach (var section in sections.Where(s =>
            s.Item1.Equals(sectionName, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var subsection in section.Item2)
            {
                var assembler = GetAssembler(subsection.Item1);
                if (assembler == null)
                {
                    program.AddRange(CompileNone(subsection.Item2));
                    continue;
                }
                var lines = SubstituteReferences(subsection.Item2, assembler, globalLabels);
                program.AddRange(assembler.Assemble(lines));
            }
        }
    }

    /// <summary>
    /// Substitutes label references in the given lines with their corresponding addresses.
    /// E.G: @myLabel -> adress 42
    /// </summary>
    /// <param name="rawLines"></param>
    /// <param name="assembler"></param>
    /// <param name="labels"></param>
    /// <returns></returns>
    private static string[] SubstituteReferences(
        string[] rawLines,
        ITargetAssembler assembler,
        IReadOnlyDictionary<string, int> labels)
    {
        if (labels.Count == 0) return rawLines;
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
        return result;
    }

    /// <summary>
    /// Gets the corresponding assembler for the given header type.
    /// </summary>
    /// <param name="header">The header type for which to get the assembler.</param>
    /// <returns>The corresponding assembler for the given header type, or null if none exists.</returns>
    private static ITargetAssembler GetAssembler(Headers header) => header switch
    {
        Headers.HEX => s_hexAssembler,
        Headers.ASM => s_asmAssembler,
        Headers.DEC => s_decAssembler,
        Headers.BIN => s_binAssembler,
        _           => null,
    };


    /// <summary>
    /// Checks if this line is a label line (ends with a colon and has at least one character before it).
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is a label line; otherwise, false.</returns>
    private static bool IsLabelLine(string line) => line.Length > 1 && line.TrimEnd().EndsWith(":");

    /// <summary>
    /// Fallback method to handle cases where no format pragma is declared. Logs an error and returns an empty array.
    /// </summary>
    /// <param name="lines">The lines to throw away/do nothing with. </param>
    /// <returns>An empty array of integers.</returns>
    private static int[] CompileNone(string[] lines)
    {
        Debug.LogError("[ScriptAssembler] No format pragma declared (#HEX / #ASM / #DEC / #BIN)." +
            " Add one at the top of the file or outside any .Code section.");
        return Array.Empty<int>();
    }

    /// <summary>
    /// Returns all sections and sub-sections of the given file.
    /// Only .Code produces bytecode; all other sections are inert.
    /// </summary>
    public static Tuple<string, Tuple<Headers, string[]>[]>[] GetSections(string scriptText)
    {
        var sections = new List<Tuple<string, Tuple<Headers, string[]>[]>>();
        var lines = scriptText.Split('\n');

        string currentSection = null;
        Headers defaultSubHeader = Headers.NONE;
        Headers currentSubHeader = Headers.NONE;
        var currentSubContent = new List<string>();
        var currentSubsections = new List<Tuple<Headers, string[]>>();

        bool InCodeSection() => currentSection != null &&
            currentSection.Equals("Code", StringComparison.OrdinalIgnoreCase);

        void FlushSubsection()
        {
            currentSubsections.Add(Tuple.Create(currentSubHeader, currentSubContent.ToArray()));
            currentSubContent = new List<string>();
        }

        void FlushSection()
        {
            if (currentSection == null) return;
            FlushSubsection();
            sections.Add(Tuple.Create(currentSection, currentSubsections.ToArray()));
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("."))
            {
                var name = line.Substring(1).Trim();

                if (InCodeSection() && Enum.TryParse<Headers>(name, ignoreCase: true, out _))
                {
                    Debug.LogWarning($"[ScriptAssembler] Inline format switch '.{name}' inside .Code is not allowed. Set the format once with a pragma in .Headers.");
                    continue;
                }

                FlushSection();
                currentSection = name;
                currentSubHeader = defaultSubHeader;
                currentSubContent = new List<string>();
                currentSubsections = new List<Tuple<Headers, string[]>>();
            }
            else if (line.StartsWith("#") && !InCodeSection())
            {
                // Pragma outside .Code — update the default format if it's a format pragma
                var parts = line.Substring(1).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && Enum.TryParse<Headers>(parts[0], ignoreCase: true, out var h))
                    defaultSubHeader = h;
            }
            else if (InCodeSection())
            {
                currentSubContent.Add(line);
            }
        }

        FlushSection();
        return sections.ToArray();
    }

    /// <summary>
    /// Injects library defines into the script headers based on the included libraries.
    /// </summary>
    /// <param name="headers"></param>
    private static void InjectLibraryDefines(ScriptHeaders headers)
    {
        foreach (string name in headers.Includes)
        {
            Type libType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { libType = Array.Find(asm.GetTypes(), t =>
                    typeof(HostCallLibrary).IsAssignableFrom(t) && !t.IsAbstract &&
                    string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)); }
                catch { }
                if (libType != null) break;
            }
            if (libType == null) continue;

            HostCallLibrary libInstance;
            try { libInstance = (HostCallLibrary)Activator.CreateInstance(libType); }
            catch { continue; }

            int libId = libInstance.LibraryID;

            if (!headers.Defines.ContainsKey(name))
                headers.Defines[name] = libId.ToString();

            foreach (var callType in Assembly.GetAssembly(libType).GetTypes())
            {
                if (!typeof(IHostCall).IsAssignableFrom(callType) || callType.IsInterface || callType.IsAbstract)
                    continue;
                var attr = callType.GetCustomAttribute<HostCallLibraryAttribute>();
                if (attr == null || attr.LibraryID != libId || headers.Defines.ContainsKey(callType.Name))
                    continue;
                try { headers.Defines[callType.Name] = ((IHostCall)Activator.CreateInstance(callType)).ID.ToString(); }
                catch { }
            }
        }
    }

    private static string ApplyDefines(string text, Dictionary<string, string> defines)
    {
        foreach (var kv in defines)
            text = Regex.Replace(text, $@"\b{Regex.Escape(kv.Key)}\b", kv.Value);
        return text;
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

    public static ScriptHeaders ParseHeaders(TextAsset script) => ParseHeaders(script.text);

    /// <summary>
    /// Parses pragmas from a .why script. Pragmas are recognized anywhere outside .Code.
    /// </summary>
    public static ScriptHeaders ParseHeaders(string scriptText)
    {
        var headers = new ScriptHeaders();
        var includes = new List<string>();
        var lines = StripComments(scriptText).Split('\n');
        bool inCodeSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("."))
            {
                inCodeSection = line.Substring(1).Trim().Equals("Code", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inCodeSection || !line.StartsWith("#")) continue;

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
                case "INCLUDE":
                    if (!string.IsNullOrWhiteSpace(value)) includes.Add(value);
                    break;
                case "DEFINE":
                    var defParts = value.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (defParts.Length == 2) headers.Defines[defParts[0]] = defParts[1].Trim();
                    break;
                case "MEMSIZE":     headers.MemSize    = (uint)ParseHexOrDec(value); break;
                case "STACKSIZE":   headers.StackSize  = (uint)ParseHexOrDec(value); break;
                case "ENTRY":
                    if (!string.IsNullOrWhiteSpace(value) && char.IsLetter(value[0]))
                        headers.EntryLabel = value;
                    else
                        headers.Entry = ParseHexOrDec(value);
                    break;
                case "TIMEOUT":     headers.Timeout     = ParseHexOrDec(value); break;
                case "TICK_RATE":   headers.TickRate    = ParseHexOrDec(value); break;
                case "LOOP":        headers.Loops       = ParseHexOrDec(value); break;
                case "DEBUG":       headers.Debug       = true; break;
                case "STRICT":      headers.Strict      = true; break;
                case "DUMP_ON_CRASH":  headers.DumpOnCrash  = true; break;
                case "NO_HOSTCALL":    headers.NoHostCall   = true; break;
                case "PROFILE":        headers.Profile      = true; break;
                case "DUMP_ON_EXIT":   headers.DumpOnExit   = true; break;
                case "STACK_PROTECT":  headers.StackProtect = true; break;
            }
        }

        headers.Includes = includes.ToArray();

        if (headers.EntryLabel != null)
            headers.Entry = ResolveEntryLabel(scriptText, headers.EntryLabel);

        return headers;
    }

    /// <summary>
    /// Resolves a label name to its global bytecode address by scanning all ASM code sections.
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
                        Headers.HEX => s_hexAssembler.Assemble(subsection.Item2),
                        Headers.DEC => s_decAssembler.Assemble(subsection.Item2),
                        Headers.BIN => s_binAssembler.Assemble(subsection.Item2),
                        _           => Array.Empty<int>(),
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

        Debug.LogWarning($"[ScriptAssembler] Could not parse header value '{value}', defaulting to 0");
        return 0;
    }
}

/// <summary>
/// Defines the possible headers for the script compiler, which determine how the code section is interpreted.
/// </summary>
public enum Headers
{
    NONE,
    ASM,
    HEX,
    DEC,
    /// <remarks>Binary format — every token is a binary integer like 00000101.</remarks>
    BIN
}
