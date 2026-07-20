using System;

namespace VirtualCPU.Opcodes
{
    public class HostCallInstruction : OpcodeInstruction
    {
        public string Name => "HOSTCALL";
        public bool Accept(int opcode) => opcode == (int)OpCodes.HOSTCALL;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            int libraryId = vCpu.Program[vCpu.ProgramCounter + 1];
            int callId    = vCpu.Program[vCpu.ProgramCounter + 2];
            vCpu.HostCallDispatcher.Dispatch(vCpu, libraryId, callId, crashHandle);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
