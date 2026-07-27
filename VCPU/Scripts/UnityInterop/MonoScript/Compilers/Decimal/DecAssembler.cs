using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assembles decimal code from a .why script file into bytecode that can be ran by the virtual cpu.
/// </summary>
public class DecAssembler : ITargetAssembler
{
    public int[] Assemble(string[] lines)
    {
        var compiledCode = new List<int>();
        foreach (var line in lines)
        {
            if (IsLabelLine(line)) continue;
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

    public int CountTokens(string line)
    {
        if (IsLabelLine(line)) return 0;
        int count = 0;
        foreach (var token in line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(token, out _)) count++;
        return count;
    }

    public string FormatAddress(int address) => address.ToString();

    private static bool IsLabelLine(string line) => line.TrimEnd().EndsWith(":");
}
