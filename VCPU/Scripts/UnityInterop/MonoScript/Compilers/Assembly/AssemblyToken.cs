using System;
using UnityEngine;
using VirtualCPU;

/// <summary>
/// Represents a token in the assembly code, consisting of a string token and its corresponding opcode instruction.
/// Holds the mnemonic representation of an assembly instruction and its associated opcode for execution in the virtual CPU.
/// </summary>
[Serializable]
public struct AssemblyToken
{
    [Tooltip("The mnemonic representation of the assembly instruction (e.g., \"MOV\", \"ADD\", \"SUB\").")]
    public string Token;

    [Tooltip("The opcode instruction associated with this token.")]
    public OpcodeInstruction Opcode;

    [Tooltip("Operands for this instruction.")]
    public AssemblyOperand[] operands;

#if UNITY_EDITOR
    [ColorUsage(false)]
    public Color InstructionColor;
    [Tooltip("Description of the assembly instruction for editor reference.")]
    public string Description;
#endif

}
