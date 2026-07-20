/// <summary>
/// Unity interop host library — library ID 0x01.
/// Provides host calls for Unity engine operations: object spawning, transforms, physics, audio, UI, and more.
/// Host calls are registered automatically via <see cref="HostCallLibraryAttribute"/> and reflection.
/// </summary>
public class UnityLib : HostCallLibrary
{
    public static readonly int ID = 0x01;
    public override int LibraryID => 0x01;
}
