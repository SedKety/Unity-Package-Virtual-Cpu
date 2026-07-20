using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// Compiles any .why script into the VM program that can be executed by the VCPU.
/// </summary>
public static class ScriptCompiler
{
    public static int[] Compile(TextAsset script)
    {
        var program = new List<int>();

        var sections = GetSections(StripComments(script.text));

        foreach (var section in sections.Where(s => s.Item1.Equals("Code", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var subsection in section.Item2)
            {
                int[] compiled = subsection.Item1 switch
                {
                    Headers.HEX  => CompileHex(subsection.Item2),
                    Headers.ASM  => CompileAsm(subsection.Item2),
                    Headers.DEC  => CompileDec(subsection.Item2),
                    Headers.BIN  => CompileBin(subsection.Item2),
                    Headers.NONE => CompileNone(subsection.Item2),
                    _            => throw new Exception($"Unknown header type: {subsection.Item1}")
                };

                program.AddRange(compiled);
            }
        }

        return program.ToArray();
    }

    private static int[] CompileHex(string[] lines)
    {
        var compiledCode = new List<int>();

        foreach (var line in lines)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (i + 1 < line.Length && line[i] == '0' && line[i + 1] == 'x')
                {
                    i += 2;
                    string fullValue = "";
                    while (i < line.Length && IsHexDigit(line[i]) && fullValue.Length < 8)
                    {
                        fullValue += line[i];
                        i++;
                    }
                    i--; // compensate for outer i++

                    if (!string.IsNullOrEmpty(fullValue) && int.TryParse(fullValue, NumberStyles.HexNumber, null, out int b))
                        compiledCode.Add(b);
                    else if (!string.IsNullOrEmpty(fullValue))
                        Debug.LogError($"Failure in parsing hex value '{fullValue}', dumping line: {line}");
                }
            }
        }
        return compiledCode.ToArray();
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private static int[] CompileAsm(string[] lines) { return Array.Empty<int>(); }

    private static int[] CompileDec(string[] lines)
    {
        var compiledCode = new List<int>();
        foreach (var line in lines)
        {
            foreach (var token in line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out int value))
                    compiledCode.Add(value);
                else
                    Debug.LogError($"DEC: could not parse '{token}' as integer");
            }
        }
        return compiledCode.ToArray();
    }

    private static int[] CompileBin(string[] lines)
    {
        var compiledCode = new List<int>();
        foreach (var line in lines)
        {
            foreach (var token in line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    compiledCode.Add(Convert.ToInt32(token, 2));
                }
                catch
                {
                    Debug.LogError($"BIN: could not parse '{token}' as binary");
                }
            }
        }
        return compiledCode.ToArray();
    }

    /// <summary>
    /// Replaced at compile-time by the method matching the standard header declared in .Headers.
    /// If no standard header is set, this section is ignored.
    /// </summary>
    private static int[] CompileNone(string[] lines) { return Array.Empty<int>(); }

    /// <summary>
    /// Returns all the sections and sub-sections of the given file.
    /// </summary>
    /// <returns>Returns (sectionName, (headerType, lines[])[])[]</returns>
    public static Tuple<string, Tuple<Headers, string[]>[]>[] GetSections(string scriptText)
    {
        var sections = new List<Tuple<string, Tuple<Headers, string[]>[]>>();
        var lines    = scriptText.Split('\n');

        string currentSection   = null;
        bool   inHeadersSection = false;
        var    headersDeclarations = new List<string>();

        Headers defaultSubHeader = Headers.NONE;
        Headers currentSubHeader = Headers.NONE;
        var     currentSubContent  = new List<string>();
        var     currentSubsections = new List<Tuple<Headers, string[]>>();

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

                if (inHeadersSection && Enum.TryParse<Headers>(name, ignoreCase: true, out _))
                {
                    headersDeclarations.Add(name);
                    continue;
                }

                if (currentSection != null && !inHeadersSection && Enum.TryParse<Headers>(name, ignoreCase: true, out var subHeader))
                {
                    FlushSubsection();
                    currentSubHeader = subHeader;
                    continue;
                }

                FlushSection();
                currentSection    = name;
                inHeadersSection  = name.Equals("headers", StringComparison.OrdinalIgnoreCase);
                headersDeclarations = new List<string>();
                currentSubHeader  = inHeadersSection ? Headers.NONE : defaultSubHeader;
                currentSubContent = new List<string>();
                currentSubsections = new List<Tuple<Headers, string[]>>();
            }
            else if (currentSection != null)
            {
                if (inHeadersSection)
                    headersDeclarations.Add(line);
                else
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
}

/// <summary>
/// Defines the possible headers for the script compiler, which determine how the code section is interpreted.
/// </summary>
public enum Headers
{
    /// <summary>Zero headers</summary>
    NONE,
    /// <summary>Read the code-section as Assembly code</summary>
    ASM,
    /// <summary>Read the code-section as opcodes written in hexadecimal (0x00, 0x0A, 0x64)</summary>
    HEX,
    /// <summary>Read the code-section as opcodes written in decimal (0, 10, 100)</summary>
    DEC,
    /// <summary>Reads the code-section as binary, please dont fucking do this (0000, 1010, 1100100)</summary>
    BIN
}
