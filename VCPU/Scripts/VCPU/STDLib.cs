using UnityEngine;
using VirtualCPU;

public class STDLib : SyscallLibrary
{
    public override byte LibraryID => 0;

    public ISyscall[] syscalls = new ISyscall[256];
    public override ISyscall GetSyscall(byte ID)
    {
        return syscalls[ID];
    }

    public override void Initialize(VCPU vCpu)
    {
        //populate syscalls with all the syscalls in this library
    }
}
