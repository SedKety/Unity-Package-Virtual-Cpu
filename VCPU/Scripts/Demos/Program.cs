using System;
using System.Linq;
using System.Reflection;

namespace VirtualCPU
{
    public class Program
    {
        static void Main(string[] args)
        {
            var program = Executables.PrintSample;

            OpcodeInstruction[] instructions = Assembly.GetAssembly(typeof(Program)).GetTypes()
                .Where(t => typeof(OpcodeInstruction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (OpcodeInstruction)Activator.CreateInstance(t))
                .ToArray();

            var cpu = new VCPU(
                program,
                instructions,
                new ConsoleLogger(),
                syscallLibraries: new SyscallLibrary[] { new STDLib() },
                loggingEnabled: false
            );
        }
    }
}
