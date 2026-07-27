using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assembles binary code from a .why script file into bytecode that can be ran by the virtual cpu.
/// </summary>
public class BinAssembler : ITargetAssembler
{
    public int[] Assemble(string[] lines)
    {
        var compiledCode = new List<int>();
        foreach (var line in lines)
        {
            if (IsLabelLine(line)) continue;
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

    public int CountTokens(string line)
    {
        if (IsLabelLine(line)) return 0;
        int count = 0;
        foreach (var token in line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            try { Convert.ToInt32(token, 2); count++; } catch { }
        }
        return count;
    }

    public string FormatAddress(int address) => Convert.ToString(address, 2).PadLeft(8, '0');

    private static bool IsLabelLine(string line) => line.TrimEnd().EndsWith(":");
}
