using System;
using VirtualCPU;

/// <summary>
/// Generates a random value in the range [EDX, ESI] and stores it in the register at index ECX.
/// Takes: EAX=0x00, EBX=<see cref="STDLibSyscall.SysRandom"/>, ECX=destination register index, EDX=min, ESI=max.
/// </summary>
[SyscallLibrary(0x00)]
public class SysRandom : ISyscall
{
    public byte ID => (byte)STDLibSyscall.SysRandom;

    private static readonly Random _random = new Random();

    public void Execute(VCPU cpu)
    {
        var destReg = cpu.Registers.GetRegisterValue((byte)Register.ECX);
        var min     = cpu.Registers.GetRegisterValue((byte)Register.EDX);
        var max     = cpu.Registers.GetRegisterValue((byte)Register.ESI);

        cpu.Registers.SetRegisterValue(destReg, (byte)_random.Next(min, max + 1));
    }
}
