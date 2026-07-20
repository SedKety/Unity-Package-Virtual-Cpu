using System;
using System.Collections.Generic;
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
    public abstract int LibraryID { get; }

    private readonly Dictionary<int, IHostCall> _calls = new Dictionary<int, IHostCall>();

    public IHostCall GetCall(int id) => _calls.TryGetValue(id, out var c) ? c : null;

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
