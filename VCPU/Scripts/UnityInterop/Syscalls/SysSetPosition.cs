using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — sets the world position of the object whose ID is in ECX to the vector held in R0/R1/R2.
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysSetPosition"/>: R0/R1/R2=X/Y/Z, ECX=object ID.
/// </summary>
[HostCallLibrary(0x01)]
public class SysSetPosition : IHostCall
{
    public byte ID => (byte)UnityLibrarySyscall.SysSetPosition;

    public void Execute(VCPU cpu)
    {
        var regs   = cpu.Registers;
        var newPos = new Vector3(
            regs.GetRegisterValue((byte)Register.R0),
            regs.GetRegisterValue((byte)Register.R1),
            regs.GetRegisterValue((byte)Register.R2));

        var id  = regs.GetRegisterValue((byte)Register.ECX);
        var obj = GameobjectService.GetObject(id);

        if (obj != null)
            obj.transform.position = newPos;
    }
}
