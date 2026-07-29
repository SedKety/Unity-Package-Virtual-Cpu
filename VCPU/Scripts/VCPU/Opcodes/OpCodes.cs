
namespace VirtualCPU
{
    /// <summary>
    /// This enum provides a non-magic way to refer to the opcodes.
    /// Each opcode is represented by a unique byte value, which the VirtualCPU will interpret during execution.
    /// </summary>
    public enum OpCodes : byte
    {
        //<-----------Program opcodes------------>

        /// <summary>
        /// Signifies the end of the program. When the VirtualCPU encounters this opcode, it will stop executing further instructions.
        /// </summary>
        END = 0x00,

        /// <summary>
        /// No operation, does nothing and moves to the next instruction.
        /// </summary>
        NOP = 0x01,

        /// <summary>
        /// Invokes a built-in core call. Always available, no library required.
        /// Format: CORECALL callIndex
        /// </summary>
        CORECALL = 0x02,

        /// <summary>
        /// Invokes a call in a user-provided host library.
        /// Format: HOSTCALL libraryIndex functionIndex
        /// </summary>
        HOSTCALL = 0x03,

        //<-----------Locational opcodes------------>

        /// <summary>
        /// Loads a value into a specified register.
        /// Format: LOAD RegisterIndex Value
        /// </summary>
        LOAD = 0x05,

        /// <summary>
        /// Jumps to a specified address in the program.
        /// Format: JMP Address
        /// </summary>
        JMP = 0x06,

        /// <summary>
        /// Moves a value from Memory to Register or Register to Memory.
        /// Format: MOV Source Destination
        /// </summary>
        MOV = 0x07,

        JNE = 0x08, // Jump if not equal
        JE = 0x09,  // Jump if equal
        JL = 0x0A,  // Jump if less
        JG = 0x0B,  // Jump if greater

        /// <summary>
        /// Pushes the return address onto the stack and jumps to the target.
        /// Format: CALL Address addrmode
        /// </summary>
        CALL = 0x0C,

        /// <summary>
        /// Pops the return address from the stack and jumps to it.
        /// Format: RET
        /// </summary>
        RET = 0x0D,

        //<-----------Arithmetic opcodes------------>

        /// <summary>
        /// Adds the values of two registers and stores the result in the first register.
        /// Format: ADD RegisterIndex1 RegisterIndex2
        /// </summary>
        ADD = 0x14,

        /// <summary>
        /// Compares the values of two registers and sets the appropriate flags.
        /// Format: CMP RegisterIndex1 RegisterIndex2
        /// </summary>
        CMP = 0x15,

        /// <summary>
        /// Subtracts the value of the second register from the first and stores the result in the first.
        /// Format: SUB RegisterIndex1 RegisterIndex2
        /// </summary>
        SUB = 0x16,

        /// <summary>
        /// Increments the value of a specified register by 1.
        /// Format: INC RegisterIndex
        /// </summary>
        INC = 0x17,

        /// <summary>
        /// Decrements the value of a specified register by 1.
        /// Format: DEC RegisterIndex
        /// </summary>
        DEC = 0x18,
    }
}
