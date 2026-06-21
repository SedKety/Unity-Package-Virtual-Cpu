using VirtualCPU;

/// <summary>
/// Host call — destroys the Unity object whose local ID is in ECX.
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysDestroy"/>: ECX=object ID.
/// </summary>
[HostCallLibrary(0x01)]
public class SysDestroy : IHostCall
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
