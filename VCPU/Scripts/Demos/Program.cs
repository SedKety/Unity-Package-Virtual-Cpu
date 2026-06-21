namespace VirtualCPU
{
    public class Program
    {
        static void Main(string[] args)
        {
            var cpu = new VCPU(
                Executables.PrintSample,
                new ConsoleLogger(),
                loggingEnabled: false
            );
        }
    }
}
