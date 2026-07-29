using System.Collections.Generic;

namespace VirtualCPU
{
    /// <summary>
    /// All configuration for a <see cref="VCPU"/> instance.
    /// Build from <see cref="Default"/> and override the fields you need.
    /// </summary>
    public struct VCPUSettings
    {
        /// <summary>
        /// Pass as <see cref="Loops"/> to loop forever.
        /// </summary>
        public const int LoopForever = int.MaxValue;

        /// <summary>
        /// Host libraries available to HOSTCALL instructions.
        /// </summary>
        public HostCallLibrary[] Libraries;

        /// <summary>
        /// Enable debug instruction logging.
        /// </summary>
        public bool LoggingEnabled;

        /// <summary>
        /// Always dump registers after execution ends.
        /// </summary>
        public bool DumpRegisters;

        /// <summary>
        /// Always dump heap memory after execution ends.
        /// </summary>
        public bool DumpMemory;

        /// <summary>
        /// Always dump flags after execution ends.
        /// </summary>
        public bool DumpFlags;

        /// <summary>
        /// Heap size in ints.
        /// </summary>
        public uint MemorySize;

        /// <summary>
        /// Maximum stack depth.
        /// </summary>
        public uint StackSize;

        /// <summary>
        /// Crash on an unknown opcode instead of silently skipping it.
        /// </summary>
        public bool Strict;

        /// <summary>
        /// Halt after this many instructions. 0 = no limit.
        /// </summary>
        public int Timeout;

        /// <summary>
        /// Program counter start address.
        /// </summary>
        public int Entry;

        /// <summary>
        /// Disable all HOSTCALL instructions.
        /// </summary>
        public bool NoHostCall;

        /// <summary>
        /// Clamp on stack overflow/underflow instead of crashing.
        /// </summary>
        public bool StackProtect;

        /// <summary>
        /// Dump registers/flags/memory when the program crashes.
        /// </summary>
        public bool DumpOnCrash;

        /// <summary>
        /// Dump registers/flags/memory on clean exit.
        /// </summary>
        public bool DumpOnExit;

        /// <summary>
        /// Log instruction count and elapsed time after execution.
        /// </summary>
        public bool Profile;

        /// <summary>
        /// When false the program is not run in the constructor;
        /// call <see cref="VCPU.Step"/> manually for tick-rate execution.
        /// </summary>
        public bool AutoRun;

        /// <summary>
        /// Number of instructions to execute per <see cref="VCPU.Step"/> call.
        /// Read by <c>ScriptExecutionUnit</c> to drive its coroutine; VCPU itself does not use this.
        /// </summary>
        public int TickRate;

        /// <summary>
        /// How many times the program runs in total.
        /// 0 = run once, N = run N times, <see cref="LoopForever"/> = loop forever.
        /// Managed by <c>ScriptExecutionUnit</c> calling <see cref="VCPU.Restart"/>.
        /// </summary>
        public int Loops;

        /// <summary>
        /// Label name -> absolute bytecode address map produced by the assembler.
        /// Null when no labels were defined or when constructing a VCPU without a script.
        /// </summary>
        public IReadOnlyDictionary<string, int> Labels;

        public static VCPUSettings Default => new VCPUSettings
        {
            Libraries = new HostCallLibrary[0],
            LoggingEnabled = true,
            MemorySize = 16,
            StackSize = 8,
            AutoRun = true,
        };
    }
}
