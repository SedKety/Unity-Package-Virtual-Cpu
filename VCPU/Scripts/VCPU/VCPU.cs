using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Debug = UnityEngine.Debug;

namespace VirtualCPU
{
    public class VCPU
    {
        #region Variables

        /// <summary>
        /// The program in ints that is being executed,
        /// this is set when the Run method is called, and is used to fetch instructions and data from it.
        /// </summary>
        private int[] _program = new int[0];

        /// <summary>
        /// The program that is being executed, this is set when the Run method is called, and is used to fetch instructions and data from it.
        /// </summary>
        public ref int[] Program => ref _program;

        /// <summary>
        /// The program counter, this is used to keep track of the current instruction being executed in the program.
        /// </summary>
        private int _pc = 0;

        /// <summary>
        /// The index of the current instruction being executed in the program,
        /// this is used to fetch instructions and data from the program, and is incremented after each instruction is executed.
        /// </summary>
        public int ProgramCounter => _pc;

        /// <summary>
        /// How many instructions are executed per <see cref="Step"/> call.
        /// Read by the runner to drive its coroutine.
        /// </summary>
        /// <remarks>0 will execute all instructions at once via the autoRun path, blocking the main thread.</remarks>
        public int TickRate => _tickRate;
        private int _tickRate;

        /// <summary>
        /// How many times the program runs in total.
        /// 0 = run once, N = run N times, <see cref="VCPUSettings.LoopForever"/> = loop forever.
        /// </summary>
        public int Loops => _loops;
        private int _loops;

        /// <summary>
        /// Tracks how many times <see cref="Restart"/> has been called.
        /// </summary>
        private int _loopCount;

        /// <summary>
        /// The ISA of the VCPU, this is used to fetch the appropriate instruction for the current instruction being executed in the program.
        /// </summary>
        private OpcodeInstruction[] _opcodeActions;

        #region Dispatchers

        /// <summary>
        /// The core call dispatcher, this is used to dispatch core calls to the appropriate methods,
        /// and is initialized in the constructor with the default core calls (SysRead, SysWrite, SysRandom).
        /// </summary>
        public CoreCallDispatcher CoreCallDispatcher => _coreCallDispatcher;
        private CoreCallDispatcher _coreCallDispatcher;

        /// <summary>
        /// The host call dispatcher, this is used to dispatch host calls to the appropriate methods,
        /// and is initialized in the constructor with the specified host libraries.
        /// </summary>
        public HostCallDispatcher HostCallDispatcher => _hostCallDispatcher;
        private HostCallDispatcher _hostCallDispatcher;

        #endregion

        /// <summary>
        /// Memory this virtual CPU has.
        /// </summary>
        public ref Memory Memory => ref _memory;
        private Memory _memory;

        /// <summary>
        /// Provides methods to get and set the values of the registers, and to set and get the flags register.
        /// </summary>
        public ref RegisterManager Registers => ref _registers;
        private RegisterManager _registers;

        #region Logging

        private bool _loggingEnabled = true;
        private bool _forceQuit = false;

        /// <summary>
        /// Handle to crash the program.
        /// </summary>
        private Action<string> _crashHandle;

        /// <summary>
        /// A logger to log messages, this is used for the "console" output in the program.
        /// </summary>
        private ILogger _logger;

        private bool _dumpRegisters;
        private bool _dumpMemory;
        private bool _dumpFlags;

        #endregion

        /// <summary>
        /// Label name -> absolute bytecode address, as produced by the assembler.
        /// Returns an empty dictionary if the program was created without labels.
        /// </summary>
        public IReadOnlyDictionary<string, int> Labels => _labels;
        private IReadOnlyDictionary<string, int> _labels;

        #region Pragmas
        /// <summary>
        /// Whether the program has crashed.
        /// </summary>
        private bool _crashed;

        /// <summary>
        /// Whether the program is running in strict mode, which will crash on unknown opcodes instead of skipping them.
        /// </summary>
        private bool _strict;

        /// <summary>
        /// The maximum number of instructions to execute before timing out and crashing the program.
        /// </summary>
        private int _timeout;

        /// <summary>
        /// Whether host calls are disabled for this instance.
        /// </summary>
        private bool _noHostCall;

        /// <summary>
        /// Whether to dump the program state (registers, memory, flags) on crash.
        /// </summary>
        private bool _dumpOnCrash;

        /// <summary>
        /// Whether to dump the program state (registers, memory, flags) on exit (non-crash).
        /// </summary>
        private bool _dumpOnExit;

        /// <summary>
        /// Whether to profile the program execution, which will log the number of instructions executed and the time taken to execute them.
        /// </summary>
        private bool _profile;

        /// <summary>
        /// The number of instructions executed so far, used for profiling and timeout.
        /// </summary>
        private int _instructionCount;

        /// <summary>
        /// Determines whether the program has already dumped its state on completion (either crash or exit).
        /// </summary>
        private bool _dumpedOnComplete;

        /// <summary>
        /// A stopwatch to measure the time taken to execute the program, used for profiling.
        /// </summary>
        private Stopwatch _profileWatch;

        /// <summary>
        /// Entry point of the program, this is set when the Run method is called, and is used to fetch instructions and data from the program.
        /// </summary>
        private int _entryPoint;

        /// <summary>
        /// Whether host calls are disabled for this instance.
        /// </summary>
        public bool NoHostCall => _noHostCall;
        #endregion

        /// <summary>
        /// Whether the program has finished executing (completed or crashed).
        /// </summary>
        public bool IsComplete => _forceQuit || _pc >= _program.Length;

        #endregion

        #region Methods

        public VCPU(int[] programArray, ILogger logger, VCPUSettings settings = default)
        {
            uint memSize = settings.MemorySize > 0 ? settings.MemorySize : 16;
            uint stkSize = settings.StackSize > 0 ? settings.StackSize : 8;

            _loggingEnabled = settings.LoggingEnabled;
            _crashHandle = Crash;
            _logger = logger;
            _dumpRegisters = settings.DumpRegisters;
            _dumpMemory = settings.DumpMemory;
            _dumpFlags = settings.DumpFlags;
            _strict = settings.Strict;
            _timeout = settings.Timeout;
            _noHostCall = settings.NoHostCall;
            _dumpOnCrash = settings.DumpOnCrash;
            _dumpOnExit = settings.DumpOnExit;
            _profile = settings.Profile;
            _entryPoint = settings.Entry;
            _tickRate = settings.TickRate;
            _loops = settings.Loops;
            _labels = settings.Labels ?? new Dictionary<string, int>();

            var libraries = settings.Libraries ?? new HostCallLibrary[0];
            Initialize(memSize, stkSize, libraries, settings.StackProtect);

            _program = programArray;
            _pc = _entryPoint;

            if (settings.AutoRun)
                Run();
        }

        private void Initialize(uint memorySize, uint stackSize, HostCallLibrary[] hostLibraries, bool stackProtect)
        {
            _memory = new Memory(new int[memorySize], stackSize, this, _crashHandle, stackProtect);
            _registers = new RegisterManager(this, _crashHandle);
            _coreCallDispatcher = new CoreCallDispatcher();
            _hostCallDispatcher = new HostCallDispatcher(hostLibraries);
            _opcodeActions = Assembly.GetAssembly(typeof(VCPU)).GetTypes()
                .Where(t => typeof(OpcodeInstruction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (OpcodeInstruction)Activator.CreateInstance(t))
                .ToArray();

            foreach (var lib in hostLibraries)
                lib.Initialize(this);
        }

        /// <summary>
        /// Executes the program to completion on the current thread.
        /// </summary>
        private void Run()
        {
            Log("Executing the program");
            _instructionCount = 0;

            if (_profile)
                _profileWatch = Stopwatch.StartNew();

            while (!IsComplete)
                ExecuteOne();

            DumpOnComplete();
        }

        /// <summary>
        /// Runs up to <paramref name="count"/> instructions then returns.
        /// Call from a coroutine or Update for tick-rate execution.
        /// <see cref="DumpOnComplete"/> fires automatically once <see cref="IsComplete"/> becomes true.
        /// </summary>
        public void Step(int count = 1)
        {
            if (_profile && _profileWatch == null)
                _profileWatch = Stopwatch.StartNew();

            int executed = 0;

            while (executed < count && !IsComplete)
            {
                ExecuteOne();
                executed++;
            }

            if (IsComplete)
                DumpOnComplete();
        }

        /// <summary>
        /// Resets execution back to the entry point.
        /// Only valid after a clean (non-crash) exit; does nothing if the program crashed.
        /// </summary>
        public void Restart()
        {
            if (_crashed)
                return;

            _forceQuit = false;
            _dumpedOnComplete = false;
            _instructionCount = 0;
            _profileWatch = null;
            _pc = _entryPoint;
            _loopCount++;
        }

        private void ExecuteOne()
        {
            int instruction = _program[_pc];
            var opcode = _opcodeActions.Where(x => x.Accept(instruction)).FirstOrDefault();

            if (opcode == null)
            {
                if (_strict)
                    Crash($"Unknown opcode {instruction} at PC {_pc}");
                else
                    _pc++;

                return;
            }

            Log($"<--(Executing {opcode.Name} instruction)-->");
            opcode.Act(this, instruction, _crashHandle);
            Log($"<--(Finished {opcode.Name} instruction)--->");
            Space();

            _instructionCount++;

            if (_timeout > 0 && _instructionCount >= _timeout)
                Crash($"Timeout: reached {_instructionCount} instructions without halting");
        }

        private void DumpOnComplete()
        {
            if (_dumpedOnComplete)
                return;

            _dumpedOnComplete = true;
            _profileWatch?.Stop();

            if (_profile)
                Log($"[Profile] {_instructionCount} instructions in {_profileWatch?.ElapsedMilliseconds ?? 0}ms");

            Space();

            bool conditional = (_dumpOnCrash && _crashed) || (_dumpOnExit && !_crashed);

            if (_dumpRegisters || conditional)
                DumpRegisters();

            Space();

            if (_dumpFlags || conditional)
                DumpFlags();

            Space();

            if (_dumpMemory || conditional)
                DumpMemory();
        }

        /// <summary>
        /// Simulates crashing the program.
        /// </summary>
        private void Crash(string errorMessage)
        {
            LogError($"The program has crashed: {errorMessage}");
            _crashed = true;
            _forceQuit = true;
        }

        private void DumpRegisters()
        {
            Log("Dumping registers:");

            for (int i = 0; i < Enum.GetValues(typeof(Register)).Length; i++)
            {
                var value = _registers.GetRegisterValue(i);
                Log($"Register {Enum.GetName(typeof(Register), i)} holds = {value}");
            }
        }

        private void DumpFlags()
        {
            Log("Dumping flags:");

            for (int i = 0; i < Enum.GetValues(typeof(Flags)).Length; i++)
            {
                var flag = (Flags)(1 << i);
                var hasFlag = Registers.FlagsRegister.HasFlag(flag);
                Log($"Flag {flag} is {(hasFlag ? "set" : "not set")}");
            }
        }

        private void DumpMemory()
        {
            Log("Dumping memory:");

            for (uint i = 0; i < _memory.HeapMemorySize; i++)
            {
                var value = _memory.GetFromMemory(i);
                Log($"Memory address {i} holds = {value}");
            }
        }

        #endregion

        #region API's

        /// <summary>
        /// Sets the program counter to a specific value.
        /// Crashes the program if the value is out of bounds.
        /// </summary>
        /// <param name="value">The value to set the program counter to.</param>
        public void SetProgramCounter(int value)
        {
            _pc = value;

            if (_pc > _program.Length)
                Crash("Tried to set the program counter to a value that is out of bounds");
        }

        /// <summary>
        /// Sets the program counter to the address of a label.
        /// </summary>
        /// <param name="label">The label whose address to set the program counter to.</param>
        public void SetProgramCounter(string label)
        {
            if (!_labels.TryGetValue(label, out int address))
            {
                Debug.LogError($"Label {label} could not be found, PC was not changed.");
                return;
            }
            SetProgramCounter(address);
        }

        public void EndProgram()
        {
            Log("Ending the program");
            _forceQuit = true;
        }

        #region Printing/Logging

        /// <summary>
        /// Prints a message to the <see cref="ILogger"/> without a new line at the end.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <remarks>Works regardless of whether _loggingEnabled is set.</remarks>
        public void Print(string message) => _logger.Log(message);

        /// <summary>
        /// Prints a character to the <see cref="ILogger"/> without a new line at the end.
        /// </summary>
        /// <param name="message">The character to print.</param>
        /// <remarks>Works regardless of whether _loggingEnabled is set.</remarks>
        public void Print(char message) => _logger.Log(message.ToString());

        /// <summary>
        /// Logs an error message to the console in red and resets the color afterward.
        /// </summary>
        /// <param name="errorMessage">The error message to log.</param>
        /// <remarks>Does NOT work if _loggingEnabled is false.</remarks>
        public void LogError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(errorMessage);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes a message to the logger if logging is enabled.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void Log(string message)
        {
            if (_loggingEnabled)
                _logger.Log(message);
        }

        /// <summary>
        /// Logs an empty line to separate instruction output in the console.
        /// </summary>
        public void Space()
        {
            if (_loggingEnabled)
                Console.WriteLine();
        }

        #endregion

        #endregion
    }
}
