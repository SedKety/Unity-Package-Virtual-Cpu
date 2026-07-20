using VirtualCPU;

/// <summary>
/// Interface for a host call. Implementations must carry a <see cref="HostCallLibraryAttribute"/>
/// so they are auto-registered by the owning <see cref="HostCallLibrary"/> during initialization.
/// Invoked with the HOSTCALL instruction followed by the library ID and this call's <see cref="ID"/>.
/// </summary>
public interface IHostCall
{
    int ID { get; }
    void Execute(VCPU cpu);
}
