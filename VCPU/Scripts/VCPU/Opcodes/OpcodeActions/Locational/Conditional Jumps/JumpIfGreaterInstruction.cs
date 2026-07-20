using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JG Destination
    /// </summary>
    public class JumpIfGreaterInstruction : OpcodeInstruction
    {
        public string Name => "JG";
        public bool Accept(int opcode) => opcode == (int)OpCodes.JG;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var flags = vCpu.Registers.FlagsRegister;
            bool isGreater = !flags.HasFlag(Flags.Signed) && !flags.HasFlag(Flags.Zero);

            if (!isGreater)
            {
                vCpu.Log("Not jumping because value is not greater");
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 2);
                return;
            }

            var destination = vCpu.Program[vCpu.ProgramCounter + 1];
            vCpu.Log($"Value is greater, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
