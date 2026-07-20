using System;
using System.Collections.Generic;
namespace VirtualCPU
{
    public enum Register : byte
    {
        R0 = 0,
        R1 = 1,
        R2 = 2,
        R3 = 3,
        R4 = 4,
        R5 = 5,
        R6 = 6,
        R7 = 7,
        R8 = 8,
        R9 = 9,
        R10 = 10,
        R11 = 11,
        R12 = 12,

        EAX = 13, //Accumulator register, used for arithmetic operations and syscall library number
        EBX = 14, //Base register, used for memory addressing and syscall local id
        ECX = 15, //Counter register, used for loops and shifts
        EDX = 16, //Data register, used for I/O operations and multiplication/division results

        ESI = 17, //Source Index register, used for string operations and memory copying
        EDI = 18, //Destination Index register, used for string operations and memory copying
    }

    /// <summary>
    /// Class that represents the registers of the virtual CPU, it provides methods to get and set the values of the registers.
    /// </summary>
    public class RegisterManager
    {
        private Dictionary<Register, int> _registerValues;
        private Flags _flagsRegister;

        private VCPU _vCpu;
        private Action<string> _crashHandle;

        public Flags FlagsRegister { get => _flagsRegister; }

        #region Methods

        #region Public Methods
        public RegisterManager(VCPU vCpu, Action<string> crashHandle = null)
        {
            _vCpu = vCpu;
            _crashHandle = crashHandle;
            InitializeRegisters();
        }

        public void SetFlagsRegister(Flags flags)
        {
            _flagsRegister = flags;
        }

        public int GetRegisterValue(int register)
        {
            if (register >= Enum.GetValues(typeof(Register)).Length)
            {
                _crashHandle?.Invoke("Tried to access a non-existent register");
                return 0;
            }
            return GetRegisterValue((Register)register);
        }

        public void SetRegisterValue(int register, int value)
        {
            if (register >= Enum.GetValues(typeof(Register)).Length)
            {
                _crashHandle?.Invoke("Tried to access a non-existent register");
                return;
            }
            SetRegisterValue((Register)register, value);
        }

        public void UpdateFlags(int lhs, int rhs, bool isSubtraction = false)
        {
            UpdateCarryFlag(lhs, rhs, _vCpu, isSubtraction);
            UpdateOverflowFlag(lhs, rhs, _vCpu, isSubtraction);
            UpdateZeroFlag(lhs, rhs, _vCpu, isSubtraction);
            UpdateSignedFlag(lhs, rhs, _vCpu, isSubtraction);
        }
        #endregion

        #region Private Methods
        private void InitializeRegisters()
        {
            _registerValues = new Dictionary<Register, int>();
            foreach (Register reg in Enum.GetValues(typeof(Register)))
                _registerValues[reg] = 0;
        }

        private void SetRegisterValue(Register register, int value)
        {
            _registerValues[register] = value;
        }

        private int GetRegisterValue(Register register)
        {
            return _registerValues[register];
        }

        #region Flags
        private void UpdateCarryFlag(int lhs, int rhs, VCPU vCpu, bool isSubtraction = false)
        {
            bool carry = isSubtraction ? lhs < rhs : (long)lhs + rhs > int.MaxValue;
            if (carry)
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister | Flags.Carry));
            else
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister & ~Flags.Carry));
        }

        private void UpdateZeroFlag(int lhs, int rhs, VCPU vCpu, bool isSubtraction = false)
        {
            int result = isSubtraction ? lhs - rhs : lhs + rhs;
            if (result == 0)
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister | Flags.Zero));
            else
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister & ~Flags.Zero));
        }

        private void UpdateOverflowFlag(int lhs, int rhs, VCPU vCpu, bool isSubtraction = false)
        {
            int result = isSubtraction ? lhs - rhs : lhs + rhs;
            bool overflow = isSubtraction
                ? ((lhs ^ rhs) & int.MinValue) != 0 && ((lhs ^ result) & int.MinValue) != 0
                : ((lhs ^ rhs) & int.MinValue) == 0 && ((lhs ^ result) & int.MinValue) != 0;

            if (overflow)
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister | Flags.Overflow));
            else
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister & ~Flags.Overflow));
        }

        private void UpdateSignedFlag(int lhs, int rhs, VCPU vCpu, bool isSubtraction = false)
        {
            int result = isSubtraction ? lhs - rhs : lhs + rhs;
            if (result < 0)
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister | Flags.Signed));
            else
                vCpu.Registers.SetFlagsRegister((Flags)(vCpu.Registers.FlagsRegister & ~Flags.Signed));
        }
        #endregion

        #endregion

        #endregion
    }
}
