using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — spawns a prefab at the position held in R0/R1/R2 and stores the object ID in the destination register.
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysSpawn"/>: R0/R1/R2=spawn position X/Y/Z, ECX=prefabId, EDX=destination register index.
/// </summary>
[HostCallLibrary(0x01)]
public class SysSpawn : IHostCall
{
    public int ID => (int)UnityLibrarySyscall.SysSpawn;

    public void Execute(VCPU cpu)
    {
        var regs = cpu.Registers;
        var pos  = new Vector3(
            regs.GetRegisterValue((int)Register.R0),
            regs.GetRegisterValue((int)Register.R1),
            regs.GetRegisterValue((int)Register.R2));

        var prefabId = regs.GetRegisterValue((int)Register.ECX);
        var destReg  = regs.GetRegisterValue((int)Register.EDX);

        var spawned = PrefabService.SpawnObjectPrefab(prefabId, pos, Quaternion.identity);
        regs.SetRegisterValue(destReg, spawned != null ? prefabId : 0);
    }
}
