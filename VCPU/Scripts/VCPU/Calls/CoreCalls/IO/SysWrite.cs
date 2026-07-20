using System;
using VirtualCPU;

/// <summary>
/// Core call — prints a register value, memory block, or immediate value to the console.
/// CORECALL <see cref="CoreCallID.SysWrite"/>: ECX=<see cref="OutputType"/>, EDX=<see cref="SourceType"/>, ESI=source.
/// </summary>
public class SysWrite : ICoreCall
{
    public int ID => (int)CoreCallID.SysWrite;

    public void Execute(VCPU cpu)
    {
        var outputType = (OutputType)cpu.Registers.GetRegisterValue((int)Register.ECX);
        var sourceType = (SourceType)cpu.Registers.GetRegisterValue((int)Register.EDX);
        var source     = cpu.Registers.GetRegisterValue((int)Register.ESI);

        Action<int> printValue = (val) =>
        {
            if (outputType == OutputType.Hex)
                cpu.Print($"0x{val:X} ");
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
            uint addr = (uint)source;
            var curVal = cpu.Memory.GetFromMemory(addr++);
            if (outputType == OutputType.String)
            {
                var sb = new System.Text.StringBuilder();
                while (curVal != '\0')
                {
                    sb.Append((char)curVal);
                    curVal = cpu.Memory.GetFromMemory(addr++);
                }
                cpu.Print(sb.ToString());
            }
            else
            {
                printValue(curVal);
            }
        }
        else if (sourceType == SourceType.ImmediateValue)
        {
            printValue(source);
        }
    }
}
