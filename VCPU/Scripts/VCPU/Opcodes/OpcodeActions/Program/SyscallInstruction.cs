using System;

namespace VirtualCPU.Opcodes
{
    public class SyscallInstruction : OpcodeInstruction
    {
        public string Name => "SYSCALL";
        public bool Accept(byte opcode) => opcode == (byte)OpCodes.SYSCALL;

        public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
        {
            byte libraryId = vCpu.Program[vCpu.ProgramCounter + 1];
            byte syscallId = vCpu.Program[vCpu.ProgramCounter + 2];
            vCpu.SyscallDispatcher.Dispatch(vCpu, libraryId, syscallId, crashHandle);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
