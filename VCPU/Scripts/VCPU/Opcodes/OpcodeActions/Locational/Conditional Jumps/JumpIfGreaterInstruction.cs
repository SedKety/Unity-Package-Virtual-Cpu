using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JG operand addrmode
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
                vCpu.SetProgramCounter(vCpu.ProgramCounter + 3);
                return;
            }

            var operand = vCpu.Program[vCpu.ProgramCounter + 1];
            bool addrmode = vCpu.Program[vCpu.ProgramCounter + 2] != 0;
            var destination = addrmode ? vCpu.Registers.GetRegisterValue(operand) : operand;
            vCpu.Log($"Value is greater, jumping to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
