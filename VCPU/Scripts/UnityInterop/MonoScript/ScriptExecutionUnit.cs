using UnityEngine;
using VirtualCPU;

public class ScriptExecutionUnit : MonoBehaviour
{
    [SerializeField] private TextAsset _scriptFile;

    [Header("VCPU settings")]
    [SerializeField] private HostCallLibrary[] _libraries;
    [Header("Memory")]
    [SerializeField] private uint _heapSize = 16;
    [SerializeField] private uint _stackSize = 8;

    [Header("Logging")]
    [SerializeField] private bool _shouldLog = true;
    [SerializeField] private bool _shouldDumpRegisters = false;
    [SerializeField] private bool _shouldDumpMemory = false;
    [SerializeField] private bool _shouldDumpFlags = false;

    private int[] _program;

    [ContextMenu("Execute")]
    public void Execute()
    {
        if (_program == null) return;
        var vcpu = new VCPU(
            _program,
            new UnityLogger(),
            _libraries,
            _shouldLog,
            _shouldDumpRegisters,
            _shouldDumpMemory,
            _shouldDumpFlags,
            _heapSize,
            _stackSize
            );
    }

    [ContextMenu("Compile")]
    public void LocalCompile() => _program = ScriptCompiler.Compile(_scriptFile);
}