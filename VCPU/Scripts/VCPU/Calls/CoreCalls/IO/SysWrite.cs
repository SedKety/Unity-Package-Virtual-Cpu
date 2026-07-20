using System;
using VirtualCPU;

/// <summary>
/// Core call — prints a register value, memory block, or immediate byte to the console.
/// CORECALL <see cref="CoreCallID.SysWrite"/>: ECX=<see cref="OutputType"/>, EDX=<see cref="SourceType"/>, ESI=source.
/// </summary>
public class SysWrite : ICoreCall
{
    public byte ID => (byte)CoreCallID.SysWrite;

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
                var sb = new System.Text.StringBuilder();
                while (curByte != '\0')
                {
                    sb.Append((char)curByte);
                    curByte = cpu.Memory.GetFromMemory(addr++);
                }
                cpu.Print(sb.ToString());
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
