using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — loads the world position of the object whose ID is in ECX into R0/R1/R2 (rounded to integer).
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysLoadPosition"/>: ECX=object ID. Returns: position in R0/R1/R2.
/// </summary>
[HostCallLibrary(0x01)]
public class SysLoadPosition : IHostCall
{
    public int ID => (int)UnityLibrarySyscall.SysLoadPosition;

    public void Execute(VCPU cpu)
    {
        var id  = cpu.Registers.GetRegisterValue((int)Register.ECX);
        var obj = GameobjectService.GetObject(id);

        if (obj == null) return;

        var pos = obj.transform.position;
        cpu.Registers.SetRegisterValue((int)Register.R0, Mathf.RoundToInt(pos.x));
        cpu.Registers.SetRegisterValue((int)Register.R1, Mathf.RoundToInt(pos.y));
        cpu.Registers.SetRegisterValue((int)Register.R2, Mathf.RoundToInt(pos.z));
    }
}
