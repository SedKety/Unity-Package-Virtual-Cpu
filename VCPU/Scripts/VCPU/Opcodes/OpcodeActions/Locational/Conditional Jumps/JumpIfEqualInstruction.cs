using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JE Destination
    /// </summary>
    public class JumpIfEqualInstruction : OpcodeInstruction
    {
        public string Name => "JE";

        public bool Accept(int opcode) => opcode == (int)OpCodes.JE;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            if (!vCpu.Registers.FlagsRegister.HasFlag(Flags.Zero))
            {
                vCpu.Log("Not jumping because the values are not equal");
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
                return;
            }

            var destination = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.Log($"Values are equal, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
