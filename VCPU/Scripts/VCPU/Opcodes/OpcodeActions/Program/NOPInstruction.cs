using System;

namespace VirtualCPU.Opcodes
{
    public class NOPInstruction : OpcodeInstruction
    {
        public string Name => "NOP";
        public bool Accept(int opcode) => opcode == (int)OpCodes.NOP;
        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle) => vCpu.SetProgramCounter(vCpu.ProgramCounter + 1);
    }
}
