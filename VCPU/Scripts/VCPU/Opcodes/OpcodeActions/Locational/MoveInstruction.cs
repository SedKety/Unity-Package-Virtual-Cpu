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

        // addrmode flag values used by source and destination operands:
        //   0 = direct memory address (static value baked in bytecode)
        //   1 = direct register
        //   2 = register-indirect: use the register's runtime value as a memory address
        public void Act(VCPU vCpu, int opcode, Action<string> crashHandle)
        {
            var registers = vCpu.Registers;

            var sourceOperand = vCpu.Program[vCpu.ProgramCounter + 1];
            int sourceMode = vCpu.Program[vCpu.ProgramCounter + 2];

            int sourceValue;
            switch (sourceMode)
            {
                case 1:
                    vCpu.Log($"MOV source: register R{sourceOperand}");
                    sourceValue = registers.GetRegisterValue(sourceOperand);
                    break;
                case 2:
                    int srcAddr = registers.GetRegisterValue(sourceOperand);
                    vCpu.Log($"MOV source: memory[R{sourceOperand}={srcAddr}]");
                    sourceValue = vCpu.Memory.GetFromMemory((uint)srcAddr);
                    break;
                default:
                    vCpu.Log($"MOV source: memory[{sourceOperand}]");
                    sourceValue = vCpu.Memory.GetFromMemory((uint)sourceOperand);
                    break;
            }

            var destination = vCpu.Program[vCpu.ProgramCounter + 3];
            int destMode = vCpu.Program[vCpu.ProgramCounter + 4];

            vCpu.Log($"MOV destination operand {destination} mode {destMode}, value {sourceValue}");

            switch (destMode)
            {
                case 1:
                    MoveToRegister(vCpu, sourceValue, destination);
                    break;
                case 2:
                    int destAddr = registers.GetRegisterValue(destination);
                    vCpu.Log($"MOV destination: memory[R{destination}={destAddr}]");
                    vCpu.Memory.WriteToMemory((uint)destAddr, sourceValue);
                    vCpu.SetProgramCounter(vCpu.ProgramCounter + 5);
                    break;
                default:
                    if (sourceMode == 0)
                    {
                        crashHandle("Invalid MOV: source and destination are both static memory addresses.");
                        return;
                    }
                    MoveToMemory(vCpu, sourceValue, destination);
                    break;
            }
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
