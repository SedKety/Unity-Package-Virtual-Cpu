using System;
using VirtualCPU;

/// <summary>
/// Core call — reads user input and stores it in a register or memory address.
/// CORECALL <see cref="CoreCallID.SysRead"/>: ECX=InputMode (0=byte, 1=char, 2=string), EDX=destination.
/// </summary>
public class SysRead : ICoreCall
{
    public byte ID => (byte)CoreCallID.SysRead;

    public void Execute(VCPU cpu)
    {
        var inputMode   = cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var destination = cpu.Registers.GetRegisterValue((byte)Register.EDX);

        if (inputMode == 2)
            cpu.Print($"Enter a value for memory address {destination}: ");
        else
            cpu.Print($"Enter a value for register {(Register)destination}: ");

        var input = Console.ReadLine();

        if (inputMode == 2)
        {
            if (!string.IsNullOrEmpty(input))
                for (int i = 0; i < input.Length; i++)
                    cpu.Memory.WriteToMemory((uint)(destination + i), (byte)input[i]);
        }
        else
        {
            byte value = 0;
            if (inputMode == 1 && !string.IsNullOrEmpty(input))
                value = (byte)input[0];
            else
                byte.TryParse(input, out value);

            cpu.Registers.SetRegisterValue(destination, value);
        }
    }
}
