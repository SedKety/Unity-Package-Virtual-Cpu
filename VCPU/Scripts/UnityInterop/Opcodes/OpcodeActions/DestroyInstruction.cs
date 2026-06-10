using UnityEngine;
using VirtualCPU;
using VirtualCPU.UnityInterop;

public class DestroyInstruction : OpcodeInstruction
{
    public string Name => "DEST";
    public bool Accept(byte opcode) => opcode == (byte)UnityOpcodes.DEST;
    public void Act(VCPU vCpu, byte opcode, System.Action<string> crashHandle)
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

        var obj = GameobjectService.GetObject(ID);

        if (obj != null)
        {
            GameobjectService.Destroy(obj);
            vCpu.Print($"Destroyed GameObject with ID {ID}");
        }
        else
        {
            vCpu.Print($"No GameObject found with ID {ID}");
        }

        vCpu.SetProgramCounter(vCpu.ProgramCounter + internalCounter + 1);
    }
}
