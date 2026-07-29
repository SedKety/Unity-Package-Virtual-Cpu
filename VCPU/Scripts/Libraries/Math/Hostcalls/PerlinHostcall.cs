using System;
using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — returns a perlin noise value for a given 2D coordinate.
/// HOSTCALL 0x02 0x00: R0=X (float bits), R1=Y (float bits).
/// OUTPUT: EAX=perlin noise value (float bits).
/// </summary>
[HostCallLibraryAttribute(0x02)]
public class PerlinHostcall : IHostCall
{
    public int ID => 0x00;

    public void Execute(VCPU cpu)
    {
        var registers = cpu.Registers;
        float x = BitConverter.Int32BitsToSingle(registers.GetRegisterValue(0));
        float y = BitConverter.Int32BitsToSingle(registers.GetRegisterValue(1));
        float perlinValue = Mathf.PerlinNoise(x, y);
        registers.SetRegisterValue(13, BitConverter.SingleToInt32Bits(perlinValue));
    }
}
