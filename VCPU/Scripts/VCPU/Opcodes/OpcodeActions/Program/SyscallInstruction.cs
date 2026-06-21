using System;

namespace VirtualCPU.Opcodes
{
    public class CoreCallInstruction : OpcodeInstruction
    {
        public string Name => "CORECALL";
        public bool Accept(byte opcode) => opcode == (byte)OpCodes.CORECALL;

        public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
        {
            byte callId = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.CoreCallDispatcher.Dispatch(vCpu, callId, crashHandle);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
        }
    }
}
