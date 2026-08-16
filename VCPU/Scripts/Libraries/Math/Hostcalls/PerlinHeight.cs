using UnityEngine;
using VirtualCPU;

/// <summary>
/// Host call — generates a height value based on Perlin noise using the provided seed and coordinates.
/// HOSTCALL 0x02 0x01: R0=x, R1=z, R2=maxHeight, R4=seed. Result is stored in EAX.
/// </summary>
[HostCallLibraryAttribute(0x02)]
public class PerlinHeight : IHostCall
{
    public int ID => 0x01;

    private const float Scale = 0.3f;

    public void Execute(VCPU cpu)
    {
        var registers = cpu.Registers;
        int seed = registers.GetRegisterValue((int)Register.R4);
        float offsetX = (seed % 997)  * 0.1f;
        float offsetZ = (seed % 1009) * 0.1f;
        float x = offsetX + registers.GetRegisterValue((int)Register.R0) * Scale;
        float z = offsetZ + registers.GetRegisterValue((int)Register.R1) * Scale;
        int maxHeight = registers.GetRegisterValue((int)Register.R2);
        float perlin = Mathf.PerlinNoise(x, z);
        int height = Mathf.RoundToInt(perlin * maxHeight);
        cpu.Log($"[PerlinHeight] x={x:F2} z={z:F2} perlin={perlin:F4} maxH={maxHeight} -> height={height}");
        registers.SetRegisterValue((int)Register.EAX, height);
    }
}
