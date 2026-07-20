using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// Loads an immediate value into a register.
    /// LOAD Register Value
    /// </summary>
    public class LoadInstruction : OpcodeInstruction
    {
        public string Name => "LOAD";

        public bool Accept(int opcode) => opcode == (int)OpCodes.LOAD;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var register = vCpu.Program[vCpu.ProgramCounter + 1];
            var value    = vCpu.Program[vCpu.ProgramCounter + 2];

            vCpu.Log($"Executing LOAD instruction: Loading value {value} into register {register}");

            vCpu.Registers.SetRegisterValue(register, value);

            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
