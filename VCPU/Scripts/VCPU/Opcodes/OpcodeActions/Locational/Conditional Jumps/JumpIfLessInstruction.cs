using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JL Destination
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
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
                return;
            }

            var destination = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.Log($"Value is less, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
