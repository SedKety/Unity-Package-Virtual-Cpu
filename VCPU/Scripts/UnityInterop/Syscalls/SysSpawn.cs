using UnityEngine;
using VirtualCPU;

/// <summary>
/// Spawns a prefab at the position held in R0/R1/R2 and stores the object ID in the destination register.
/// Takes: EAX=0x01, EBX=<see cref="UnityLibrarySyscall.SysSpawn"/>, R0/R1/R2=spawn position X/Y/Z, ECX=prefabId, EDX=destination register index.
/// Returns: object ID written into the register at index EDX; 0 if spawn failed.
/// </summary>
[SyscallLibrary(0x01)]
public class SysSpawn : ISyscall
{
    public byte ID => (byte)UnityLibrarySyscall.SysSpawn;

    public void Execute(VCPU cpu)
    {
        var regs = cpu.Registers;
        var pos  = new Vector3(
            regs.GetRegisterValue((byte)Register.R0),
            regs.GetRegisterValue((byte)Register.R1),
            regs.GetRegisterValue((byte)Register.R2));

        var prefabId = regs.GetRegisterValue((byte)Register.ECX);
        var destReg  = regs.GetRegisterValue((byte)Register.EDX);

        var spawned = PrefabService.SpawnObjectPrefab(prefabId, pos, Quaternion.identity);
        regs.SetRegisterValue(destReg, spawned != null ? prefabId : (byte)0);
    }
}
