using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// JMP DestinationAddress
    /// </summary>
    public class JumpInstruction : OpcodeInstruction
    {
        public string Name => "JMP";

        public bool Accept(int opcode) => opcode == (int)OpCodes.JMP;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var localPC = vCpu.ProgramCounter + 1;
            var destination = vCpu.Program[localPC];
            vCpu.Log($"Jumping from {localPC} to: {destination}");
            vCpu.SetProgramCounter(destination);
        }
    }
}
