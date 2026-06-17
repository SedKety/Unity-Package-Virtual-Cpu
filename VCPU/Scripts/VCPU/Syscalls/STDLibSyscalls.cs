using VirtualCPU;

/// <summary>
/// Syscall IDs for the standard library (library ID 0x00).
/// Load 0x00 into EAX and one of these values into EBX before executing SYSCALL.
/// </summary>
public enum STDLibSyscall : byte
{
    /// <summary>ECX=InputMode (0=byte, 1=char, 2=string), EDX=destination register index or memory address.</summary>
    SysRead   = 0x00,

    /// <summary>ECX=<see cref="OutputType"/>, EDX=<see cref="SourceType"/>, ESI=source (register index / memory address / immediate byte).</summary>
    SysWrite  = 0x01,

    /// <summary>ECX=destination register index, EDX=min, ESI=max. Result written into the register at index ECX.</summary>
    SysRandom = 0x02,
}
