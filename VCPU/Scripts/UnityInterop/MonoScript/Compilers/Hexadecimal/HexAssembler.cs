using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// A simple assembler that converts hexadecimal values in a script into an array of integers.
/// </summary>
public class HexAssembler : ITargetAssembler
{
    public int[] Assemble(string[] lines)
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
                    i--; //compensate for outer i++

                    if (!string.IsNullOrEmpty(fullValue) && int.TryParse(fullValue, NumberStyles.HexNumber, null, out int b))
                        compiledCode.Add(b);
                    else if (!string.IsNullOrEmpty(fullValue))
                        Debug.LogError($"Failure in parsing hex value '{fullValue}', dumping line: {line}");
                }
            }
        }
        return compiledCode.ToArray();
    }

    public int CountTokens(string line)
    {
        int count = 0;
        for (int i = 0; i < line.Length - 1; i++)
            if (line[i] == '0' && line[i + 1] == 'x') count++;
        return count;
    }

    public string FormatAddress(int address) => $"0x{address:X2}";

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') ||
        (c >= 'a' && c <= 'f') ||
        (c >= 'A' && c <= 'F');
}
