using UnityEngine;
using VirtualCPU;

/// <summary>
/// This represents all the syscalls for a specific library (e.g: Base lib, Unity Interop, Custom)
/// </summary>
public abstract class SyscallLibrary
{
    /// <summary>
    /// ID of this library
    /// </summary>
    public abstract byte LibraryID { get; }

    /// <summary>
    /// Gets the syscall at the given ID
    /// </summary>
    /// <param name="ID">The ID of the syscall.</param>
    /// <returns>The syscall at the given ID.</returns>
    public abstract ISyscall GetSyscall(byte ID);

    public abstract void Initialize(VCPU vCpu);
}
