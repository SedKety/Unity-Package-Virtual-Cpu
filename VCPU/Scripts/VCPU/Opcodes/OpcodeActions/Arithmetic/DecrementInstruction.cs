using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// DEC Register
    /// </summary>
    public class DecrementInstruction : OpcodeInstruction
    {
        public string Name => "DEC";
        public bool Accept(int opcode) => opcode == (int)OpCodes.DEC;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var pc = vCpu.ProgramCounter;
            var registers = vCpu.Registers;
            vCpu.Log($"Decrementing R{vCpu.Program[pc + 1]} CurValue = {registers.GetRegisterValue(vCpu.Program[pc + 1])}");

            registers.SetRegisterValue(vCpu.Program[pc + 1], registers.GetRegisterValue(vCpu.Program[pc + 1]) - 1);

            vCpu.Log($"NewValue = {registers.GetRegisterValue(vCpu.Program[pc + 1])}");
            vCpu.SetProgramCounter(pc + 2);
        }
    }
}
