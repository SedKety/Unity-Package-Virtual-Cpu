/// <summary>
/// Call IDs for built-in core calls. Pass the ID as the single operand of a CORECALL instruction.
/// </summary>
public enum CoreCallID : byte
{
    /// <summary>ECX=InputMode (0=byte, 1=char, 2=string), EDX=destination register index or memory address.</summary>
    SysRead   = 0x00,

    /// <summary>ECX=<see cref="OutputType"/>, EDX=<see cref="SourceType"/>, ESI=source (register index / memory address / immediate byte).</summary>
    SysWrite  = 0x01,

    /// <summary>ECX=destination register index, EDX=min, ESI=max. Result written into the register at index ECX.</summary>
    SysRandom = 0x02,
}
