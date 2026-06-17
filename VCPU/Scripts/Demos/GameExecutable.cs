namespace VirtualCPU
{
    // RpgExample and GuessingGame used PRT/IPT/RND opcodes which are now syscalls.
    // They need to be rewritten using SYSCALL with STDLib (EAX=0x00).
    public static class GameExecutable
    {
    }
}
