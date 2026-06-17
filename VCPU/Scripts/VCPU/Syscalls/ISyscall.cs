using VirtualCPU;

/// <summary>
/// Interface for a syscall, this is used to define a syscall that can be executed by the VCPU.
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