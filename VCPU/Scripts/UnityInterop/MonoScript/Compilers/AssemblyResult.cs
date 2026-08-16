using System.Collections.Generic;

/// <summary>
/// The output of <see cref="ScriptAssembler.Assemble"/>: the compiled bytecode together
/// with a map of every label name to its absolute bytecode address.
/// </summary>
public class AssemblyResult
{
    /// <summary>
    /// Compiled bytecode ready to hand to the VCPU.
    /// </summary>
    public int[] Program { get; }

    /// <summary>
    /// Label name -> absolute bytecode address, collected from all code subsections.
    /// Case-insensitive. Empty when the script defines no labels.
    /// </summary>
    public IReadOnlyDictionary<string, int> Labels { get; }

    public AssemblyResult(int[] program, Dictionary<string, int> labels)
    {
        Program = program;
        Labels = labels;
    }
}
