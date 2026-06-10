using System;
using UnityEngine;
using VirtualCPU;
using VirtualCPU.UnityInterop;

/// <summary>
/// Sets position from three consecutive registers (R0..R2) into the object specified by LocalObjectID.
/// Written in the format: SPOS SourceType(Immediate, reg, mem) Source(register index, memory location)
/// </summary>
public class SetPositionInstruction : OpcodeInstruction
{
    public string Name => "SPOS";

    public bool Accept(byte opcode) => opcode == (byte)UnityOpcodes.SPOS;

    public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
    {
        var internalCounter = 0;
        SourceType objectId = (SourceType)vCpu.Program[vCpu.ProgramCounter + ++internalCounter];
        var regs = vCpu.Registers;
        var newPos = new Vector3(regs.GetRegisterValue(0), regs.GetRegisterValue(1), regs.GetRegisterValue(2));

        var ID = 0;
        switch (objectId)
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
            targetedObject.transform.position = newPos;
            Debug.LogWarning($"Object(ID) {objectId} set to {newPos}");
        }
        else
        {
            Debug.LogWarning($"No object found under the following ID: {objectId}");
        }

        vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
    }
}
