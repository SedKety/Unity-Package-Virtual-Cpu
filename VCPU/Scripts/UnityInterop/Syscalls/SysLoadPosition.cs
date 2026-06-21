using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — loads the world position of the object whose ID is in ECX into R0/R1/R2 (rounded to integer).
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysLoadPosition"/>: ECX=object ID. Returns: position in R0/R1/R2.
/// </summary>
[HostCallLibrary(0x01)]
public class SysLoadPosition : IHostCall
{
    public byte ID => (byte)UnityLibrarySyscall.SysLoadPosition;

    public void Execute(VCPU cpu)
    {
        var id  = cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var obj = GameobjectService.GetObject(id);

        if (obj == null) return;

        var pos = obj.transform.position;
        cpu.Registers.SetRegisterValue((byte)Register.R0, (byte)Mathf.RoundToInt(pos.x));
        cpu.Registers.SetRegisterValue((byte)Register.R1, (byte)Mathf.RoundToInt(pos.y));
        cpu.Registers.SetRegisterValue((byte)Register.R2, (byte)Mathf.RoundToInt(pos.z));
    }
}
