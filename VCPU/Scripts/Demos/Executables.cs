using System;
using System.Text;

namespace VirtualCPU
{
    public static class Executables
    {
        public static byte[] SampleProgram = new byte[]
        {
            (byte)OpCodes.INC, (byte)Register.R0,
            (byte)OpCodes.MOV, (byte)Register.R0, 1, (byte)Register.R1, 1,
            (byte)OpCodes.ADD, (byte)Register.R0, (byte)Register.R1,
            (byte)OpCodes.CMP, (byte)Register.R1, (byte)Register.R0,
            (byte)OpCodes.END
        };

        public static byte[] JumpIfEqualSample = new byte[]
        {
            (byte)OpCodes.LOAD, (byte)Register.R0, 5,
            (byte)OpCodes.LOAD, (byte)Register.R1, 5,
            (byte)OpCodes.CMP,  (byte)Register.R0, (byte)Register.R1,
            (byte)OpCodes.JE,   15,
            (byte)OpCodes.LOAD, (byte)Register.R2, 0,
            (byte)OpCodes.END,
            (byte)OpCodes.LOAD, (byte)Register.R2, 1,
            (byte)OpCodes.END
        };

        public static byte[] LoopSample = new byte[]
        {
            (byte)OpCodes.LOAD, (byte)Register.R0, 0,
            (byte)OpCodes.LOAD, (byte)Register.R1, 10,
            (byte)OpCodes.CMP,  (byte)Register.R0, (byte)Register.R1,
            (byte)OpCodes.JE,   15,
            (byte)OpCodes.INC,  (byte)Register.R0,
            (byte)OpCodes.JMP,  6,
            (byte)OpCodes.END
        };

        // Print 42 as decimal via SysWrite core call
        // ECX=Decimal, EDX=Register, ESI=R0
        public static byte[] PrintSample = new byte[]
        {
            (byte)OpCodes.LOAD,    (byte)Register.R0,  42,
            (byte)OpCodes.LOAD,    (byte)Register.ECX, (byte)OutputType.Decimal,
            (byte)OpCodes.LOAD,    (byte)Register.EDX, (byte)SourceType.Register,
            (byte)OpCodes.LOAD,    (byte)Register.ESI, (byte)Register.R0,
            (byte)OpCodes.CORECALL, (byte)CoreCallID.SysWrite,
            (byte)OpCodes.END
        };

        public static byte[] OnlyBytes = new byte[]
        {
            3, 2, 0, 2, 0, 1, 0, 0
        };
    }
}
