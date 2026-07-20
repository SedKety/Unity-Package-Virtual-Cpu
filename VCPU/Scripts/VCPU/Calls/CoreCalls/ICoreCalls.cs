using VirtualCPU;

/// <summary>
/// Interface for a built-in core call. Implementations are auto-discovered by <see cref="CoreCallDispatcher"/>
/// and are always available — no registration or library required.
/// Invoked with the CORECALL instruction followed by the call's <see cref="ID"/>.
/// </summary>
public interface ICoreCall
{
    int ID { get; }
    void Execute(VCPU cpu);
}
