using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// CALL operand addrmode — pushes the return address onto the stack and jumps to the target.
    /// </summary>
    public class CallInstruction : OpcodeInstruction
    {
        public string Name => "CALL";
        public bool Accept(int opcode) => opcode == (int)OpCodes.CALL;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var operand = vCpu.Program[vCpu.ProgramCounter + 1];
            bool isReg = vCpu.Program[vCpu.ProgramCounter + 2] != 0;
            var destination = isReg ? vCpu.Registers.GetRegisterValue(operand) : operand;

            vCpu.Memory.PushToStack(vCpu.ProgramCounter + 3);
            vCpu.Log($"CALL -> {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
