using System;
using VirtualCPU;

/// <summary>
/// Core call — reads user input and stores it in a register or memory address.
/// CORECALL <see cref="CoreCallID.SysRead"/>: ECX=InputMode (0=int, 1=char, 2=string), EDX=destination.
/// </summary>
public class SysRead : ICoreCall
{
    public int ID => (int)CoreCallID.SysRead;

    public void Execute(VCPU cpu)
    {
        var inputMode   = cpu.Registers.GetRegisterValue((int)Register.ECX);
        var destination = cpu.Registers.GetRegisterValue((int)Register.EDX);

        if (inputMode == 2)
            cpu.Print($"Enter a value for memory address {destination}: ");
        else
            cpu.Print($"Enter a value for register {(Register)destination}: ");

        var input = Console.ReadLine();

        if (inputMode == 2)
        {
            if (!string.IsNullOrEmpty(input))
                for (int i = 0; i < input.Length; i++)
                    cpu.Memory.WriteToMemory((uint)(destination + i), input[i]);
        }
        else
        {
            int value = 0;
            if (inputMode == 1 && !string.IsNullOrEmpty(input))
                value = input[0];
            else
                int.TryParse(input, out value);

            cpu.Registers.SetRegisterValue(destination, value);
        }
    }
}
