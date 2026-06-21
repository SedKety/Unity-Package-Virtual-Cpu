using System;
using System.Linq;
using System.Reflection;
using VirtualCPU;

/// <summary>
/// Base class for a user-provided host call library. A library groups a set of related <see cref="IHostCall"/>
/// implementations under a single library ID. Subclasses only need to provide <see cref="LibraryID"/>;
/// host calls are discovered and registered automatically via reflection using <see cref="HostCallLibraryAttribute"/>.
/// </summary>
public abstract class HostCallLibrary
{
    public abstract byte LibraryID { get; }

    private readonly IHostCall[] _calls = new IHostCall[256];

    public IHostCall GetCall(byte id) => _calls[id];

    public void Initialize(VCPU vCpu)
    {
        var types = Assembly.GetAssembly(GetType()).GetTypes()
            .Where(t => typeof(IHostCall).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract
                     && t.GetCustomAttribute<HostCallLibraryAttribute>()?.LibraryID == LibraryID);

        foreach (var type in types)
        {
            var call = (IHostCall)Activator.CreateInstance(type);
            _calls[call.ID] = call;
        }
    }
}
