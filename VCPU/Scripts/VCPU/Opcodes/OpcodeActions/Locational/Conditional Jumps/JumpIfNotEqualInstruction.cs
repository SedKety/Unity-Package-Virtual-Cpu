using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JNE operand isRegister
    /// </summary>
    public class JumpIfNotEqualInstruction : OpcodeInstruction
    {
        public string Name => "JNE";
        public bool Accept(int opcode) => opcode == (int)OpCodes.JNE;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            if (vCpu.Registers.FlagsRegister.HasFlag(Flags.Zero))
            {
                vCpu.Log("Not jumping because values are equal");
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
                return;
            }

            var operand = vCpu.Program[vCpu.ProgramCounter + 1];
            bool isRegister = vCpu.Program[vCpu.ProgramCounter + 2] != 0;
            var destination = isRegister ? vCpu.Registers.GetRegisterValue(operand) : operand;
            vCpu.Log($"Values are not equal, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
