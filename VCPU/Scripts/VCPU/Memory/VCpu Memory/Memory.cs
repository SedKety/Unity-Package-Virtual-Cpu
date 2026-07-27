using System;
using System.Collections.Generic;

namespace VirtualCPU
{
    /// <summary>
    /// This acts as the interface between the virtual CPU and the memory, it provides methods to read and write to memory,
    /// through the stack and heap.
    /// </summary>
    public class Memory
    {
        #region Variables
        private int[] _heapMemory;
        public int HeapMemorySize => _heapMemory.Length;

        private VCPU _vCPU;
        private Action<string> _crashHandle;

        private Stack<int> _stack = new Stack<int>();
        private uint _stackSize;
        private bool _stackProtect;
        #endregion

        #region Methods

        #region Logging
        private string SegmentationFaultMessage(uint adress) => $"Segmentation fault: Attempted to access address {adress}, which is outside the bounds of the memory.";
        private string StackflowMessage(bool isOverFlow) => $"Stack {(isOverFlow ? "overflow" : "underflow")}: Attempted to {(isOverFlow ? "push onto" : "pop from")} a {(isOverFlow ? "full" : "empty")} stack.";
        #endregion

        public Memory(int[] heapMem, uint stackSize, VCPU vCpu, Action<string> crashHandle, bool stackProtect = false)
        {
            _heapMemory = heapMem;
            _vCPU = vCpu;
            _crashHandle = crashHandle;
            _stack = new Stack<int>((int)stackSize);
            _stackSize = stackSize;
            _stackProtect = stackProtect;
        }

        #region Stack

        /// <remarks> ! Can StackOverflow ! </remarks>
        public void PushToStack(int value)
        {
            if (_stack.Count >= _stackSize)
            {
                if (!_stackProtect) _crashHandle(StackflowMessage(true));
                return;
            }
            _stack.Push(value);
        }

        public int PeekStack()
        {
            if (_stack.Count == 0)
            {
                if (!_stackProtect) _crashHandle(StackflowMessage(false));
                return 0;
            }
            return _stack.Peek();
        }

        /// <remarks> ! Can StackUnderflow ! </remarks>
        public int PopFromStack()
        {
            if (_stack.Count == 0)
            {
                if (!_stackProtect) _crashHandle(StackflowMessage(false));
                return 0;
            }
            return _stack.Pop();
        }
        #endregion

        #region Heap
        /// <remarks> ! Can SegFault !</remarks>
        public void WriteToMemory(uint adress, int value)
        {
            if (adress >= _heapMemory.Length)
            {
                _crashHandle(SegmentationFaultMessage(adress));
                return;
            }
            _heapMemory[adress] = value;
        }

        /// <remarks> ! Can SegFault !</remarks>
        public int GetFromMemory(uint adress)
        {
            if (adress >= _heapMemory.Length)
            {
                _crashHandle(SegmentationFaultMessage(adress));
                return 0;
            }
            return _heapMemory[adress];
        }

        public uint GetMemorySize() => (uint)_heapMemory.Length;
        #endregion

        #endregion
    }
}
