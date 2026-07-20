using System;

namespace VirtualCPU.Opcodes
{
    public class EndInstruction : OpcodeInstruction
    {
        public string Name => "END";
        public bool Accept(int opcode) => opcode == (int)OpCodes.END;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            vCpu.EndProgram();
        }
    }
}
