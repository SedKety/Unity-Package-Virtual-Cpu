using VirtualCPU;

/// <summary>
/// Unity interop library — library ID 0x01.
/// Provides syscalls for Unity engine operations: object spawning, transforms, physics, audio, UI, and more.
/// Syscalls are registered automatically via <see cref="SyscallLibraryAttribute"/> and reflection.
/// </summary>
public class UnityLib : SyscallLibrary
{
    public static readonly byte ID = 0x01;
    public override byte LibraryID => 0x01;
}
