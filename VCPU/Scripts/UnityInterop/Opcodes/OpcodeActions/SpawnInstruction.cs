using System;
using UnityEngine;
using VirtualCPU;
using VirtualCPU.UnityInterop;

/// <summary>
/// Implements the SPWN opcode, which spawns a prefab at specified coordinates and stores the local object ID in a register.
/// Location is determined by three consecutive registers (R0..R2) at the time of execution.
/// Written in the format: SPWN PrefabID DestReg
/// </summary>
public class SpawnOperation : OpcodeInstruction
{
    public string Name => "SPWN";

    public bool Accept(byte opcode) => opcode == (byte)UnityOpcodes.SPWN;

    public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
    {
        var regs = vCpu.Registers;
        var dest = new Vector3(regs.GetRegisterValue(0), regs.GetRegisterValue(1), regs.GetRegisterValue(2));
        var prefabId = vCpu.Program[vCpu.ProgramCounter + 1];
        var destReg = vCpu.Program[vCpu.ProgramCounter + 2];

        vCpu.Print($"SPWN: prefabId={prefabId} pos={dest} destReg=R{destReg}");

        GameObject prefab = PrefabService.GetPrefab(prefabId);
        Debug.Log($"SPWN: PrefabService.GetPrefab({prefabId}) => {(prefab != null ? prefab.name : "null")}");

        GameObject spawnedObject = PrefabService.SpawnObjectPrefab(prefabId, dest, Quaternion.identity);

        if (spawnedObject == null)
        {
            Debug.LogWarning($"SPWN: Spawn failed for prefab id={prefabId}");
            regs.SetRegisterValue(destReg, 0);
        }
        else
        {
            Debug.Log($"SPWN: Spawned object '{spawnedObject.name}' instanceId={prefabId}");

            regs.SetRegisterValue(destReg, prefabId);
        }

        vCpu.SetProgramCounter((byte)(vCpu.ProgramCounter + 3));
    }
}
