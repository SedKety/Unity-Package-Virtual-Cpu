using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// Jump if Not Equal — jumps to the destination if the Zero flag is not set.
    /// Format: JNE Destination
    /// </summary>
    public class JumpIfNotEqualInstruction : OpcodeInstruction
    {
        public string Name => "JNE";
        public bool Accept(byte opcode) => opcode == (byte)OpCodes.JNE;

        public void Act(VCPU vCpu, byte opcode, Action<string> crashHandle)
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
