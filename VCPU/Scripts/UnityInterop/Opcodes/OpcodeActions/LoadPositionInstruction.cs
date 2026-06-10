using System;
using UnityEngine;
using VirtualCPU;
using VirtualCPU.UnityInterop;

/// <summary>
/// Loads position of LocalObjectID into three consecutive registers (R0..R2).
/// Written in the format: LPOS LocalObjectID
/// </summary>
public class LoadPositionInstruction : OpcodeInstruction
{
    public string Name => "LPOS";

    public bool Accept(byte opcode) => opcode == (byte)UnityOpcodes.LPOS;

    public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
    {
        var internalCounter = 0;
        SourceType objectIdSource = (SourceType)vCpu.Program[vCpu.ProgramCounter + ++internalCounter];
        var regs = vCpu.Registers;

        var ID = 0;
        switch (objectIdSource)
        {
            case SourceType.ImmediateValue:
                ID = vCpu.Program[vCpu.ProgramCounter + ++internalCounter];
                break;
            case SourceType.Register:
                var regId = vCpu.Program[vCpu.ProgramCounter + ++internalCounter];
                ID = regs.GetRegisterValue(regId);
                break;
            case SourceType.Memory:
                ID = vCpu.Memory.GetFromMemory(vCpu.Program[vCpu.ProgramCounter + ++internalCounter]);
                break;
        }

        var targetedObject = GameobjectService.GetObject(ID);

        if (targetedObject != null)
        {
            var pos = targetedObject.transform.position;
            regs.SetRegisterValue(0, (byte)Mathf.RoundToInt(pos.x));
            regs.SetRegisterValue(1, (byte)Mathf.RoundToInt(pos.y));
            regs.SetRegisterValue(2, (byte)Mathf.RoundToInt(pos.z));
            Debug.LogWarning($"Object(ID) {ID} position {pos} loaded into registers R0,R1,R2");
        }
        else
        {
            Debug.LogWarning($"LPOS instruction: No object found with ID {ID}");
        }

        vCpu.SetProgramCounter(vCpu.ProgramCounter + ++internalCounter);
    }
}
