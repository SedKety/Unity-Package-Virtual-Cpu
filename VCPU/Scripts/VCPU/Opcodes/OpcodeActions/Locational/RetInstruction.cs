using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// RET — pops the return address from the stack and jumps to it.
    /// </summary>
    public class RetInstruction : OpcodeInstruction
    {
        public string Name => "RET";
        public bool Accept(int opcode) => opcode == (int)OpCodes.RET;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var returnAddress = vCpu.Memory.PopFromStack();
            vCpu.Log($"RET -> {returnAddress}");
            vCpu.SetProgramCounter(returnAddress);
        }
    }
}
