using System;
using VirtualCPU;

/// <summary>
/// Prints the value of a register, a block of memory, or an immediate byte to the console.
/// Takes: EAX=0x00, EBX=<see cref="STDLibSyscall.SysWrite"/>, ECX=<see cref="OutputType"/>, EDX=<see cref="SourceType"/>, ESI=source (register index / memory address / immediate byte).
/// </summary>
[SyscallLibrary(0x00)]
public class SysWrite : ISyscall
{
    public byte ID => (byte)STDLibSyscall.SysWrite;

    public void Execute(VCPU cpu)
    {
        var outputType = (OutputType)cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var sourceType = (SourceType)cpu.Registers.GetRegisterValue((byte)Register.EDX);
        var source     = cpu.Registers.GetRegisterValue((byte)Register.ESI);

        Action<byte> printValue = (val) =>
        {
            if (outputType == OutputType.Hex)
                cpu.Print($"0x{val:X2} ");
            else if (outputType == OutputType.Decimal)
                cpu.Print(val.ToString() + " ");
            else
                cpu.Print((char)val);
        };

        if (sourceType == SourceType.Register)
        {
            printValue(cpu.Registers.GetRegisterValue(source));
        }
        else if (sourceType == SourceType.Memory)
        {
            uint addr = source;
            var curByte = cpu.Memory.GetFromMemory(addr++);
            if (outputType == OutputType.String)
            {
                while (curByte != '\0')
                {
                    printValue(curByte);
                    curByte = cpu.Memory.GetFromMemory(addr++);
                }
            }
            else
            {
                printValue(curByte);
            }
        }
        else if (sourceType == SourceType.ImmediateValue)
        {
            printValue(source);
        }
    }
}
