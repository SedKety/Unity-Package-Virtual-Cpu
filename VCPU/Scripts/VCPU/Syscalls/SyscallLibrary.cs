using System;
using System.Linq;
using System.Reflection;
using VirtualCPU;

/// <summary>
/// Base class for a syscall library. A library groups a set of related <see cref="ISyscall"/> implementations
/// under a single library ID (loaded into EAX before a SYSCALL instruction).
/// Subclasses only need to provide <see cref="LibraryID"/>; syscalls are discovered and registered
/// automatically at runtime via reflection using <see cref="SyscallLibraryAttribute"/>.
/// </summary>
public abstract class SyscallLibrary
{
    /// <summary>The ID of this library, placed in EAX before a SYSCALL instruction.</summary>
    public abstract byte LibraryID { get; }

    private readonly ISyscall[] _syscalls = new ISyscall[256];

    /// <summary>Returns the syscall registered at the given local ID, or null if none.</summary>
    public ISyscall GetSyscall(byte ID) => _syscalls[ID];

    /// <summary>
    /// Scans the assembly for all <see cref="ISyscall"/> types tagged with
    /// <see cref="SyscallLibraryAttribute"/> matching this library's ID and registers them.
    /// </summary>
    public void Initialize(VCPU vCpu)
    {
        var syscallTypes = Assembly.GetAssembly(GetType()).GetTypes()
            .Where(t => typeof(ISyscall).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract
                     && t.GetCustomAttribute<SyscallLibraryAttribute>()?.LibraryID == LibraryID);

        foreach (var type in syscallTypes)
        {
            var syscall = (ISyscall)Activator.CreateInstance(type);
            _syscalls[syscall.ID] = syscall;
        }
    }
}
