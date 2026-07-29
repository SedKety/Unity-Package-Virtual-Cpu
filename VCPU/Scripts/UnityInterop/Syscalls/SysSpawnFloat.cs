using System;
using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — spawns a prefab at a float-encoded position.
/// HOSTCALL 0x01 <see cref="UnityLibrarySyscall.SysSpawnFloat"/>: R0/R1/R2=spawn position X/Y/Z (float bits), ECX=prefabId, EDX=destination register index.
/// </summary>
[HostCallLibrary(0x01)]
public class SysSpawnFloat : IHostCall
{
    public int ID => (int)UnityLibrarySyscall.SysSpawnFloat;

    public void Execute(VCPU cpu)
    {
        var regs = cpu.Registers;
        var pos = new Vector3(
            BitConverter.Int32BitsToSingle(regs.GetRegisterValue((int)Register.R0)),
            BitConverter.Int32BitsToSingle(regs.GetRegisterValue((int)Register.R1)),
            BitConverter.Int32BitsToSingle(regs.GetRegisterValue((int)Register.R2)));

        var prefabId = regs.GetRegisterValue((int)Register.ECX);
        var destReg  = regs.GetRegisterValue((int)Register.EDX);

        var spawned = PrefabService.SpawnObjectPrefab(prefabId, pos, Quaternion.identity);
        regs.SetRegisterValue(destReg, spawned != null ? prefabId : 0);
    }
}
