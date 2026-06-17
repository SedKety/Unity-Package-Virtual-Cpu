using VirtualCPU;

/// <summary>
/// Destroys the Unity object whose local ID is in ECX.
/// Takes: EAX=0x01, EBX=<see cref="UnityLibrarySyscall.SysDestroy"/>, ECX=object ID.
/// </summary>
[SyscallLibrary(0x01)]
public class SysDestroy : ISyscall
{
    public byte ID => (byte)UnityLibrarySyscall.SysDestroy;

    public void Execute(VCPU cpu)
    {
        var id  = cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var obj = GameobjectService.GetObject(id);

        if (obj != null)
            GameobjectService.Destroy(obj);
    }
}
