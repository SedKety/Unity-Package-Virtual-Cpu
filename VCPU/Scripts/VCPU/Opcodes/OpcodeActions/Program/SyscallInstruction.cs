using System;

namespace VirtualCPU.Opcodes
{
    public class CoreCallInstruction : OpcodeInstruction
    {
        public string Name => "CORECALL";
        public bool Accept(int opcode) => opcode == (int)OpCodes.CORECALL;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            int callId = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.CoreCallDispatcher.Dispatch(vCpu, callId, crashHandle);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
        }
    }
}
