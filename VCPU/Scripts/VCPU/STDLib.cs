using VirtualCPU;

/// <summary>
/// Core library — library ID 0x00.
/// Provides core I/O syscalls: SysRead, SysWrite, SysRandom.
/// Syscalls are registered automatically via <see cref="SyscallLibraryAttribute"/> and reflection.
/// </summary>
public class STDLib : SyscallLibrary
{
    public static readonly STDLib Instance = new STDLib();
    public override byte LibraryID => 0x00;

    public static readonly byte ID = 0x00;
}
