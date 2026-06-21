using System;

namespace VirtualCPU.Opcodes
{
    public class HostCallInstruction : OpcodeInstruction
    {
        public string Name => "HOSTCALL";
        public bool Accept(byte opcode) => opcode == (byte)OpCodes.HOSTCALL;

        public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
        {
            byte libraryId = vCpu.Program[vCpu.ProgramCounter + 1];
            byte callId    = vCpu.Program[vCpu.ProgramCounter + 2];
            vCpu.HostCallDispatcher.Dispatch(vCpu, libraryId, callId, crashHandle);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
