using System;
using System.Text;

namespace VirtualCPU
{
    public static class Executables
    {
        public static int[] SampleProgram = new int[]
        {
            (int)OpCodes.INC, (int)Register.R0,
            (int)OpCodes.MOV, (int)Register.R0, 1, (int)Register.R1, 1,
            (int)OpCodes.ADD, (int)Register.R0, (int)Register.R1,
            (int)OpCodes.CMP, (int)Register.R1, (int)Register.R0,
            (int)OpCodes.END
        };

        public static int[] JumpIfEqualSample = new int[]
        {
            (int)OpCodes.LOAD, (int)Register.R0, 5,
            (int)OpCodes.LOAD, (int)Register.R1, 5,
            (int)OpCodes.CMP,  (int)Register.R0, (int)Register.R1,
            (int)OpCodes.JE,   15,
            (int)OpCodes.LOAD, (int)Register.R2, 0,
            (int)OpCodes.END,
            (int)OpCodes.LOAD, (int)Register.R2, 1,
            (int)OpCodes.END
        };

        public static int[] LoopSample = new int[]
        {
            (int)OpCodes.LOAD, (int)Register.R0, 0,
            (int)OpCodes.LOAD, (int)Register.R1, 10,
            (int)OpCodes.CMP,  (int)Register.R0, (int)Register.R1,
            (int)OpCodes.JE,   15,
            (int)OpCodes.INC,  (int)Register.R0,
            (int)OpCodes.JMP,  6,
            (int)OpCodes.END
        };

        public static int[] PrintSample = new int[]
        {
            (int)OpCodes.LOAD,    (int)Register.R0,  42,
            (int)OpCodes.LOAD,    (int)Register.ECX, (int)OutputType.Decimal,
            (int)OpCodes.LOAD,    (int)Register.EDX, (int)SourceType.Register,
            (int)OpCodes.LOAD,    (int)Register.ESI, (int)Register.R0,
            (int)OpCodes.CORECALL, (int)CoreCallID.SysWrite,
            (int)OpCodes.END
        };

        public static int[] OnlyBytes = new int[]
        {
            3, 2, 0, 2, 0, 1, 0, 0
        };
    }
}
