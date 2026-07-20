using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// Instruction to add the values of two registers and store the result in the destination register.
    /// Written in the format: ADD DestinationRegister, SourceRegister1
    /// </summary>
    public class AddInstruction : OpcodeInstruction
    {
        public string Name => "ADD";

        public bool Accept(int opcode) => opcode == (int)OpCodes.ADD;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var lhs = vCpu.Registers.GetRegisterValue(vCpu.Program[vCpu.ProgramCounter + 1]);
            var rhs = vCpu.Registers.GetRegisterValue(vCpu.Program[vCpu.ProgramCounter + 2]);

            vCpu.Log($"Adding {lhs} and {rhs} from registers {vCpu.Program[vCpu.ProgramCounter + 1]} and {vCpu.Program[vCpu.ProgramCounter + 2]}");

            vCpu.Registers.UpdateFlags(lhs, rhs);

            var result = lhs + rhs;
            vCpu.Registers.SetRegisterValue(vCpu.Program[vCpu.ProgramCounter + 1], result);

            vCpu.Log($"Result of addition: {result} stored in register {vCpu.Program[vCpu.ProgramCounter + 1]}");
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
