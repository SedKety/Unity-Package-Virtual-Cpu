using System.Collections;
using UnityEngine;
using VirtualCPU;

/// <summary>
/// This class is responsible for executing a compiled script using a virtual CPU (VCPU). 
/// It allows for the execution of scripts with various settings, including memory size, stack size, logging options, and more. 
/// The class can execute scripts either in a single run or in a coroutine for repeated execution based on specified tick rates and loop counts.
/// </summary>
public class ScriptExecutionUnit : MonoBehaviour
{
    #region Variables
    [SerializeField] private TextAsset _scriptFile;
    [SerializeField] private AssemblyTokenHolder _tokenHolder;

    [Header("VCPU Settings")]
    [SerializeField] private HostCallLibrary[] _libraries;

    [Header("Memory")]
    [SerializeField] private uint _heapSize = 16;
    [SerializeField] private uint _stackSize = 8;

    [Header("Logging")]
    [SerializeField] private bool _shouldLog = true;
    [SerializeField] private bool _shouldDumpRegisters = false;
    [SerializeField] private bool _shouldDumpMemory = false;
    [SerializeField] private bool _shouldDumpFlags = false;

    private VCPU _vcpu;
    private AssemblyResult _assembled;
    private ScriptHeaders _headers;

    #endregion

    #region Methods
    /// <summary>
    /// Sends a message to the virtual CPU (VCPU) to set the program counter to a specific label.
    /// </summary>
    /// <param name="label">The label to set the program counter to.</param>
    public void SendMessage(string label) => _vcpu.SetProgramCounter(label);

    /// <summary>
    /// Executes the compiled script using the virtual CPU (VCPU).
    /// </summary>
    [ContextMenu("Execute")]
    public void Execute()
    {
        if (_assembled == null)
        {
            Debug.LogWarning("[ScriptExecutionUnit] No compiled program. Run Assemble first.");
            return;
        }

        bool usesCoroutine = _headers != null && (_headers.TickRate > 0 || _headers.Loops != 0);

        if (usesCoroutine)
            StartCoroutine(ExecuteCoroutine());
        else
            _vcpu = CreateVCPU(autoRun: true);
    }


    private IEnumerator ExecuteCoroutine()
    {
        _vcpu = CreateVCPU(autoRun: false);
        int tickRate = _headers.TickRate > 0 ? _headers.TickRate : int.MaxValue;
        int maxRuns = _headers.Loops == 0 ? 1 : _headers.Loops;
        int runCount = 1;

        while (true)
        {
            while (!_vcpu.IsComplete)
            {
                _vcpu.Step(tickRate);
                yield return null;
            }

            bool forever = maxRuns == VCPUSettings.LoopForever;
            bool hasMore = forever || runCount < maxRuns;

            if (!hasMore)
                break;

            _vcpu.Restart();
            runCount++;
        }
    }

    private VCPU CreateVCPU(bool autoRun)
    {
        var settings = new VCPUSettings
        {
            Libraries = _libraries,
            LoggingEnabled = _shouldLog,
            DumpRegisters = _shouldDumpRegisters,
            DumpMemory = _shouldDumpMemory,
            DumpFlags = _shouldDumpFlags,
            MemorySize = _headers != null && _headers.MemSize > 0 ? _headers.MemSize : _heapSize,
            StackSize = _headers != null && _headers.StackSize > 0 ? _headers.StackSize : _stackSize,
            Entry = _headers?.Entry ?? 0,
            Strict = _headers?.Strict ?? false,
            Timeout = _headers?.Timeout ?? 0,
            NoHostCall = _headers?.NoHostCall ?? false,
            StackProtect = _headers?.StackProtect ?? false,
            DumpOnCrash = _headers?.DumpOnCrash ?? false,
            DumpOnExit = _headers?.DumpOnExit ?? false,
            Profile = _headers?.Profile ?? false,
            TickRate = _headers?.TickRate ?? 0,
            Loops = _headers?.Loops ?? 0,
            Labels = _assembled?.Labels,
            AutoRun = autoRun,
        };

        return new VCPU(_assembled.Program, new UnityLogger(), settings);
    }

    #endregion
}
