using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualCPU;

/// <summary>
/// This class is responsible for executing a compiled script using a virtual CPU (VCPU).
/// It allows for the execution of scripts with various settings, including memory size, stack size, logging options, and more.
/// The class can execute scripts either in a single run or in a coroutine for repeated execution based on specified tick rates and loop counts.
/// </summary>
public class ScriptExecutionUnit : MonoBehaviour
{
    /// <summary>
    /// Represents a value definition, which can be used to override a #define in the script.
    /// E.G. #define MAX_HEALTH 100 -> MOV R0, MAX_HEALTH. R0 now stores 100.
    /// </summary>
    [Serializable]
    public struct ValueDefinition
    {
        public string Name;
        public string Value;
    }

    #region Variables
    [SerializeField] private TextAsset _scriptFile;
    [SerializeField] private AssemblyTokenHolder _tokenHolder;

    [Header("Defines")]
    [SerializeField] private List<ValueDefinition> _defineOverrides = new List<ValueDefinition>();

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

    private Coroutine _process;
    #endregion

    #region Methods
    /// <summary>
    /// Sends a message to the virtual CPU (VCPU) to set the program counter to a specific label.
    /// </summary>
    /// <param name="label">The label to set the program counter to.</param>
    public void SendMessage(string label) => _vcpu.SetProgramCounter(label);

    /// <summary>
    /// Executes the assembled script using the virtual CPU (VCPU).
    /// </summary>
    [ContextMenu("Execute")]
    public void Execute()
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in _defineOverrides)
            if (!string.IsNullOrEmpty(d.Name))
                overrides[d.Name] = d.Value;

        _assembled = ScriptAssembler.Assemble(_scriptFile, overrides.Count > 0 ? overrides : null);
        _headers = ScriptAssembler.ParseHeaders(_scriptFile);

        bool usesCoroutine = _headers.TickRate > 0 || _headers.Loops != 0;

#if UNITY_EDITOR
        if (!Application.isPlaying && _headers.TickRate != 0)
        {
            Debug.LogWarning($"[ScriptExecutionUnit] Script is dependent on tickrate, can only be tested in playmode.");
            return;
        }
#endif


        if (usesCoroutine)
            _process = StartCoroutine(ExecuteCoroutine());
        else
            _vcpu = CreateVCPU(autoRun: true);
    }

    [ContextMenu("Stop")]
    public void StopProcess()
    {
        if (_process != null)
        {
            StopCoroutine(_process);
            _process = null;
        }
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

    /// <summary>
    /// Gets the library defined in the script with #library {library_name}
    /// </summary>
    /// <param name="typeName">The name of the class hosting the library. </param>
    /// <returns>The library found under the name. </returns>
    private static HostCallLibrary ResolveLibrary(string typeName)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(t => typeof(HostCallLibrary).IsAssignableFrom(t)
                              && !t.IsAbstract
                              && t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            Debug.LogWarning($"[ScriptExecutionUnit] #include '{typeName}' no HostCallLibrary subclass with that name found.");
            return null;
        }
        return (HostCallLibrary)Activator.CreateInstance(type);
    }

    /// <summary>
    /// Creates a virtual cpu instance with the given settings and script 
    /// </summary>
    /// <param name="autoRun">Argument for the <see cref="VCPU"/> on whether or not to start when created</param>
    /// <returns>The created <see cref="VCPU"/> instance. </returns>
    private VCPU CreateVCPU(bool autoRun)
    {
        var libraries = _headers?.Includes
            .Select(ResolveLibrary)
            .Where(l => l != null)
            .ToArray() ?? Array.Empty<HostCallLibrary>();

        var settings = new VCPUSettings
        {
            Libraries = libraries,
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
