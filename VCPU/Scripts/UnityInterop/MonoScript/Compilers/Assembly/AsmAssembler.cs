using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VirtualCPU;

// TODO: Fix this slop, gotta get a linker, a proper parser, and a proper tokenizer.
// REMARKS: Works for now but gotta fix this sometime later, if you do see this send me a DM and i'll fix it right away, im just procrastinating - 16/06/2026

/// <summary>
/// Assembles the given assembly source code into an array of integers representing the program.
/// </summary>
public class AsmAssembler : ITargetAssembler
{
    /// <summary>
    /// Maps mnemonics to their expected operand formats.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_Formats =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "END", Array.Empty<string>() },
        { "NOP", Array.Empty<string>() },
        { "CORECALL", new[] { "val" } },
        { "HOSTCALL", new[] { "val", "val" } },
        { "LOAD", new[] { "val", "val" } },
        { "JMP", new[] { "val", "addrmode" } },
        { "MOV", new[] { "val", "addrmode", "val", "addrmode" } },
        { "JNE", new[] { "val", "addrmode" } },
        { "JE", new[] { "val", "addrmode" } },
        { "JL", new[] { "val", "addrmode" } },
        { "JG",   new[] { "val", "addrmode" } },
        { "CALL", new[] { "val", "addrmode" } },
        { "RET",  Array.Empty<string>() },
        { "ADD", new[] { "val", "val" } },
        { "CMP", new[] { "val", "val" } },
        { "SUB", new[] { "val", "val" } },
        { "INC", new[] { "val" } },
        { "DEC", new[] { "val" } },
    };

    /// <summary>
    /// Turns the given assembly source code into an array of integers representing the program.
    /// </summary>
    /// <param name="lines">The assembly source code lines.</param>
    /// <returns>An array of integers representing the assembled program.</returns>
    public int[] Assemble(string[] lines)
    {
        var labels = CollectLabels(lines);
        var output = new List<int>();

        foreach (var line in lines)
        {
            if (IsLabel(line)) continue;

            var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var mnemonic = parts[0];
            var userOperands = parts.Length > 1
                ? parts[1].Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray()
                : Array.Empty<string>();

            if (!s_Formats.TryGetValue(mnemonic, out var format))
            {
                Debug.LogError($"[AsmAssembler] Unknown mnemonic '{mnemonic}'");
                continue;
            }

            if (!Enum.TryParse<OpCodes>(mnemonic, ignoreCase: true, out var opcode))
            {
                Debug.LogError($"[AsmAssembler] No OpCodes entry for mnemonic '{mnemonic}'");
                continue;
            }

            output.Add((int)opcode);

            int userIdx = 0;
            string lastRaw = null;

            foreach (var slot in format)
            {
                if (slot == "addrmode")
                {
                    output.Add(IsRegMode(lastRaw));
                }
                else
                {
                    if (userIdx >= userOperands.Length)
                    {
                        Debug.LogError($"[AsmAssembler] Too few operands for '{mnemonic}' (expected {format.Count(s => s != "addrmode")}, got {userOperands.Length})");
                        output.Add(0);
                    }
                    else
                    {
                        lastRaw = userOperands[userIdx++];
                        output.Add(ParseValue(lastRaw, labels));
                    }
                }
            }
        }

        return output.ToArray();
    }

    public int CountTokens(string line)
    {
        if (IsLabel(line)) return 0;
        var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        var mnemonic = parts.Length > 0 ? parts[0] : string.Empty;
        return s_Formats.TryGetValue(mnemonic, out var fmt) ? 1 + fmt.Length : 0;
    }

    public string FormatAddress(int address) => address.ToString();

    /// <summary>
    /// Maps labels to their output stream address.
    /// Each instruction advances by the same number of ints it emits in Assemble().
    /// </summary>
    /// <param name="lines">The assembly source code lines.</param>
    /// <returns>A dictionary mapping label names to their corresponding addresses.</returns>
    public Dictionary<string, int> BuildLabelMap(string[] lines) => CollectLabels(lines);

    /// <summary>
    /// Collects labels from the given assembly source code lines and maps them to their corresponding addresses in the output stream.
    /// </summary>
    /// <param name="lines">The assembly source code lines.</param>
    /// <returns>A dictionary mapping label names to their corresponding addresses.</returns>
    private Dictionary<string, int> CollectLabels(string[] lines)
    {
        var labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int address = 0;

        foreach (var line in lines)
        {
            if (IsLabel(line))
            {
                labels[StripLabel(line)] = address;
                continue;
            }

            var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var mnemonic = parts.Length > 0 ? parts[0] : string.Empty;

            if (s_Formats.TryGetValue(mnemonic, out var fmt))
                address += 1 + fmt.Length;
        }

        return labels;
    }

    /// <summary>
    /// Parses the given raw operand token into an integer value, resolving labels and registers as needed.
    /// </summary>
    /// <param name="raw">The raw operand token to parse.</param>
    /// <param name="labels">A dictionary mapping label names to their corresponding addresses.</param>
    /// <returns>The integer value of the parsed operand.</returns>
    /// <remarks>This stores floats in integers as their bitwise representation.</remarks>
    private int ParseValue(string raw, Dictionary<string, int> labels)
    {
        //[addr] / [label] / [R0] = explicit memory reference, strip brackets then parse inner.
        string s = raw.Length >= 2 && raw[0] == '[' && raw[raw.Length - 1] == ']'
            ? raw.Substring(1, raw.Length - 2)
            : raw;

        //Only treat as a register if the token starts with a letter, this prevents numeric
        //memory addresses like "0", "1" from accidentally matching enum values.
        if (s.Length > 0 && char.IsLetter(s[0]) &&
            Enum.TryParse<Register>(s, ignoreCase: true, out var reg))
            return (int)reg;

        if (labels.TryGetValue(s, out int labelAddr))
            return labelAddr;

        if ((s.StartsWith("0x") || s.StartsWith("0X")) &&
            int.TryParse(s.Substring(2), NumberStyles.HexNumber, null, out int hex))
            return hex;

        if (int.TryParse(s, out int dec))
            return dec;

        bool hasFloatSuffix = s.EndsWith("f", StringComparison.OrdinalIgnoreCase);
        bool hasDecimalPoint = s.Contains('.');
        if (hasFloatSuffix || hasDecimalPoint)
        {
            string floatStr = hasFloatSuffix ? s.Substring(0, s.Length - 1) : s;
            if (float.TryParse(floatStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                return BitConverter.SingleToInt32Bits(f);
        }

        Debug.LogError($"[AsmAssembler] Could not parse operand '{raw}'");
        return 0;
    }

    /// <summary>
    /// Only identifiers starting with a letter can be register names. This prevents numeric
    /// strings like "0", "11" from matching via Enum.TryParse's integer value parsing.
    /// </summary>
    /// <param name="raw">The raw operand token to evaluate.</param>
    /// <returns>True if the raw operand token is a register name, otherwise, false.</returns>
    private bool IsRegisterName(string raw) =>
        raw != null && raw.Length > 0 && char.IsLetter(raw[0]) &&
        Enum.TryParse<Register>(raw, ignoreCase: true, out _);

    /// <summary>
    /// Returns the addrmode flag value for the given raw operand token:
    /// 0 = static memory address (bare number, label, or [number]) 
    /// 1 = direct register (bare register name like R0, EAX)
    /// 2 = register indirect ([R0] = use register's runtime value as memory address)
    /// </summary>
    /// <param name="raw">The raw operand token to evaluate.</param>
    /// <returns>The addrmode flag value for the given operand token.</returns>
    private int IsRegMode(string raw)
    {
        if (raw == null) return 0;
        if (IsBracketed(raw))
            return IsRegisterName(Unbracket(raw)) ? 2 : 0;
        return IsRegisterName(raw) ? 1 : 0;
    }

    private static bool IsBracketed(string raw) => raw != null && raw.Length >= 2 && raw[0] == '[' && raw[raw.Length - 1] == ']';
    private static string Unbracket(string raw) => raw.Substring(1, raw.Length - 2);

    private bool IsLabel(string line) => line.TrimEnd().EndsWith(":");
    private string StripLabel(string line) => line.TrimEnd().TrimEnd(':').Trim();
}
