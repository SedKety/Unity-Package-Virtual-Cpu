using System;
using System.Linq;
using System.Reflection;
using VirtualCPU;

/// <summary>
/// Discovers and dispatches built-in core calls.
/// All <see cref="ICoreCalls"/> implementations in the VCPU assembly are registered automatically at construction.
/// </summary>
public class CoreCallDispatcher
{
    private readonly ICoreCall[] _calls = new ICoreCall[256];

    public CoreCallDispatcher()
    {
        var types = Assembly.GetAssembly(typeof(CoreCallDispatcher)).GetTypes()
            .Where(t => typeof(ICoreCall).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in types)
        {
            var call = (ICoreCall)Activator.CreateInstance(type);
            _calls[call.ID] = call;
        }
    }

    public void Dispatch(VCPU cpu, byte callId, Action<string> crashHandle)
    {
        var call = _calls[callId];
        if (call == null)
        {
            crashHandle($"Unknown core call 0x{callId:X2}");
            return;
        }
        call.Execute(cpu);
    }
}
