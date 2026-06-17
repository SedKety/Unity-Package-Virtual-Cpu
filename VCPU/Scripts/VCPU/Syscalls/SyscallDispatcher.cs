using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualCPU;

public class SyscallDispatcher 
{
    private readonly Dictionary<int, SyscallLibrary> _table = new Dictionary<int, SyscallLibrary>();

    public SyscallDispatcher(SyscallLibrary[] libraries)
    {
        foreach (var library in libraries)
            _table.Add(library.LibraryID, library);
    }

    public void Dispatch(VCPU cpu)
    {
        //Get the library number from EAX and the local id from EBX 
    }
}
