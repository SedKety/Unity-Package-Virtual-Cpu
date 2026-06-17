using System;

/// <summary>
/// Marks an <see cref="ISyscall"/> implementation as belonging to a specific <see cref="SyscallLibrary"/>.
/// The library ID must match the <see cref="SyscallLibrary.LibraryID"/> of the library that should own this syscall.
/// Registration happens automatically via reflection in <see cref="SyscallLibrary.Initialize"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SyscallLibraryAttribute : Attribute
{
    public byte LibraryID { get; }
    public SyscallLibraryAttribute(byte libraryID) => LibraryID = libraryID;
}
