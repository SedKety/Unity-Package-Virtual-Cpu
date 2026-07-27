public interface ITargetAssembler
{
    int[] Assemble(string[] lines);

    /// <summary>
    /// Returns the number of bytecode tokens a single (non-label) line emits.
    /// Used by the shared label pre-processor to compute label addresses.
    /// </summary>
    int CountTokens(string line);

    /// <summary>
    /// Formats an integer address as a literal this assembler can parse,
    /// e.g. "0x06" for HEX, "6" for DEC/ASM, "00000110" for BIN.
    /// </summary>
    string FormatAddress(int address);
}
