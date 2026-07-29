using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JMP operand addrmode
    /// </summary>
    public class JumpInstruction : OpcodeInstruction
    {
        public string Name => "JMP";

        public bool Accept(int opcode) => opcode == (int)OpCodes.JMP;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var operand     = vCpu.Program[vCpu.ProgramCounter + 1];
            bool addrmode = vCpu.Program[vCpu.ProgramCounter + 2] != 0;
            var destination = addrmode ? vCpu.Registers.GetRegisterValue(operand) : operand;
            vCpu.Log($"Jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
