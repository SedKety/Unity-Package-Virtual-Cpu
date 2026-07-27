using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JL operand isRegister
    /// </summary>
    public class JumpIfLessInstruction : OpcodeInstruction
    {
        public string Name => "JL";

        public bool Accept(int opcode) => opcode == (int)OpCodes.JL;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            if (!vCpu.Registers.FlagsRegister.HasFlag(Flags.Signed))
            {
                vCpu.Log("Not jumping because the value is not less");
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
                return;
            }

            var operand = vCpu.Program[vCpu.ProgramCounter + 1];
            bool isRegister = vCpu.Program[vCpu.ProgramCounter + 2] != 0;
            var destination = isRegister ? vCpu.Registers.GetRegisterValue(operand) : operand;
            vCpu.Log($"Value is less, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
