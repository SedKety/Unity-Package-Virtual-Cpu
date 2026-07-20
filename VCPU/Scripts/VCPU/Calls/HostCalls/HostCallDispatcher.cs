using System;
using System.Collections.Generic;
using VirtualCPU;

/// <summary>
/// Routes HOSTCALL instructions to the correct user-provided <see cref="HostCallLibrary"/> and <see cref="IHostCall"/>.
/// </summary>
public class HostCallDispatcher
{
    private readonly Dictionary<int, HostCallLibrary> _libraries = new Dictionary<int, HostCallLibrary>();

    public HostCallDispatcher(HostCallLibrary[] libraries)
    {
        foreach (var lib in libraries)
            _libraries.Add(lib.LibraryID, lib);
    }

    public void Dispatch(VCPU cpu, int libraryId, int callId, Action<string> crashHandle)
    {
        if (!_libraries.TryGetValue(libraryId, out var library))
        {
            crashHandle($"Unknown host library 0x{libraryId:X2}");
            return;
        }

        var call = library.GetCall(callId);
        if (call == null)
        {
            crashHandle($"Unknown host call 0x{callId:X2} in library 0x{libraryId:X2}");
            return;
        }

        call.Execute(cpu);
    }
}
