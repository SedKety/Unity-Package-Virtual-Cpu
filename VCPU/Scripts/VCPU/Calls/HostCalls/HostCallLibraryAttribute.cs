using System;

/// <summary>
/// Marks an <see cref="IHostCall"/> implementation as belonging to a specific <see cref="HostCallLibrary"/>.
/// The library ID must match the <see cref="HostCallLibrary.LibraryID"/> of the owning library.
/// Registration happens automatically via reflection in <see cref="HostCallLibrary.Initialize"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HostCallLibraryAttribute : Attribute
{
    public int LibraryID { get; }
    public HostCallLibraryAttribute(int libraryID) => LibraryID = libraryID;
}
