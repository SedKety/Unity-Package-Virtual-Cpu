using System;

namespace VirtualCPU.Opcodes
{
    /// <summary>
    /// Move instruction, moves a value from a source to a destination.
    /// MOV source isSourceRegister destination isDestinationRegister
    /// </summary>
    public class MoveInstruction : OpcodeInstruction
    {
        public string Name => "MOV";
        public bool Accept(int opcode) => opcode == (int)OpCodes.MOV;

        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var registers = vCpu.Registers;

            var sourceOperand      = vCpu.Program[vCpu.ProgramCounter + 1];
            bool sourceIsRegister  = vCpu.Program[vCpu.ProgramCounter + 2] != 0;

            vCpu.Log($"Executing MOV instruction with source operand {sourceOperand} (IsRegister: {sourceIsRegister})");
            var sourceValue = sourceIsRegister
                ? registers.GetRegisterValue(sourceOperand)
                : vCpu.Memory.GetFromMemory((uint)sourceOperand);

            var destination           = vCpu.Program[vCpu.ProgramCounter + 3];
            bool destinationIsRegister = vCpu.Program[vCpu.ProgramCounter + 4] != 0;

            vCpu.Log($"Destination operand {destination} (IsRegister: {destinationIsRegister})");

            if (destinationIsRegister)
                MoveToRegister(vCpu, sourceValue, destination);
            else if (sourceIsRegister)
                MoveToMemory(vCpu, sourceValue, destination);
            else
                crashHandle("Invalid MOV instruction: both sourceValue and destination cannot be memory.");
        }

        private void MoveToRegister(VCPU vCpu, int source, int destination)
        {
            vCpu.Log($"Moving value {source} to register R{destination}");
            vCpu.Registers.SetRegisterValue(destination, source);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 5);
        }

        private void MoveToMemory(VCPU vCpu, int source, int destination)
        {
            vCpu.Log($"Moving value {source} to memory address {destination}");
            vCpu.Memory.WriteToMemory((uint)destination, source);
            vCpu.SetProgramCounter(vCpu.ProgramCounter + 5);
        }
    }
}
