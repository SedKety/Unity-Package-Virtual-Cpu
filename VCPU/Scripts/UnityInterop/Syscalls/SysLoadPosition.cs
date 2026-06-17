using UnityEngine;
using VirtualCPU;

/// <summary>
/// Loads the world position of the object whose ID is in ECX into R0/R1/R2 (rounded to integer).
/// Takes: EAX=0x01, EBX=<see cref="UnityLibrarySyscall.SysLoadPosition"/>, ECX=object ID.
/// Returns: position written into R0/R1/R2.
/// </summary>
[SyscallLibrary(0x01)]
public class SysLoadPosition : ISyscall
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
