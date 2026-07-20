using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JNE Destination
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
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
                return;
            }

            var destination = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.Log($"Values are not equal, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
