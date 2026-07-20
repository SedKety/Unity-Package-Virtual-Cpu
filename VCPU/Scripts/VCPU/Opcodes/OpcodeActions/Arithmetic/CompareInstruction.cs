using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// Instruction to compare the values of two registers and update the flags register accordingly.
    /// CMP Register1 Register2
    /// </summary>
    public class CompareInstruction : OpcodeInstruction
    {
        public string Name => "CMP";

        public bool Accept(int opcode) => opcode == (int)OpCodes.CMP;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var lhs = vCpu.Registers.GetRegisterValue(vCpu.Program[vCpu.ProgramCounter + 1]);
            var rhs = vCpu.Registers.GetRegisterValue(vCpu.Program[vCpu.ProgramCounter + 2]);

            vCpu.Log($"Comparing {lhs} and {rhs} from registers {vCpu.Program[vCpu.ProgramCounter + 1]} and {vCpu.Program[vCpu.ProgramCounter + 2]}");

            vCpu.Registers.UpdateFlags(lhs, rhs, isSubtraction: true);

            for (int i = 0; i < Enum.GetValues(typeof(Flags)).Length; i++)
            {
                var flag = (Flags)(1 << i);
                var hasFlag = vCpu.Registers.FlagsRegister.HasFlag(flag);
                vCpu.Log($"Flag {flag} is {(hasFlag ? "set" : "not set")}");
            }

            vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
        }
    }
}
