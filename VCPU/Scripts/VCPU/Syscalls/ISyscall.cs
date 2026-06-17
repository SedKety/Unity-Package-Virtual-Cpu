using VirtualCPU;

/// <summary>
/// Interface for a syscall. Implementations must also carry a <see cref="SyscallLibraryAttribute"/>
/// so they are auto-registered by the owning <see cref="SyscallLibrary"/> during initialization.
/// </summary>
public interface ISyscall
{
    /// <summary>
    /// ID of the syscall, this is local for the library this syscall is in, and not global for all syscalls.
    /// </summary>
    public byte ID { get; }

    /// <summary>
    /// Execute the syscall, this is called by the VCPU when the syscall is invoked.
    /// </summary>
    /// <param name="cpu">The virtual CPU instance that is executing the syscall.</param>
    public void Execute(VCPU cpu);
}