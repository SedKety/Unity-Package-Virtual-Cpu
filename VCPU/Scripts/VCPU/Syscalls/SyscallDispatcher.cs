using System;
using System.Collections.Generic;
using VirtualCPU;

public class SyscallDispatcher
{
    private readonly Dictionary<byte, SyscallLibrary> _table = new Dictionary<byte, SyscallLibrary>();

    public SyscallDispatcher(SyscallLibrary[] libraries)
    {
        foreach (var library in libraries)
            _table.Add(library.LibraryID, library);
    }

    public void Dispatch(VCPU cpu, byte libraryId, byte syscallId, Action<string> crashHandle)
    {
        if (!_table.TryGetValue(libraryId, out var library))
        {
            crashHandle($"Unknown syscall library 0x{libraryId:X2}");
            return;
        }

        var syscall = library.GetSyscall(syscallId);
        if (syscall == null)
        {
            crashHandle($"Unknown syscall 0x{syscallId:X2} in library 0x{libraryId:X2}");
            return;
        }

        syscall.Execute(cpu);
    }
}
