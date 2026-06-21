using System;
using VirtualCPU;

/// <summary>
/// Core call — generates a random value in [EDX, ESI] and stores it in the register at index ECX.
/// CORECALL <see cref="CoreCallID.SysRandom"/>: ECX=destination register index, EDX=min, ESI=max.
/// </summary>
public class SysRandom : ICoreCall
{
    public byte ID => (byte)CoreCallID.SysRandom;

    private static readonly Random _random = new Random();

    public void Execute(VCPU cpu)
    {
        var destReg = cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var min     = cpu.Registers.GetRegisterValue((byte)Register.EDX);
        var max     = cpu.Registers.GetRegisterValue((byte)Register.ESI);

        cpu.Registers.SetRegisterValue(destReg, (byte)_random.Next(min, max + 1));
    }
}
