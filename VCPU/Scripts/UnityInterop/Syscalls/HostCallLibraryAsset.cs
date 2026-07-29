using UnityEngine;

/// <summary>
/// ScriptableObject wrapper for a <see cref="HostCallLibrary"/>. Create assets that
/// extend this class and drag them into the Libraries list on <see cref="ScriptExecutionUnit"/>.
/// Decorate subclasses with <see cref="HostCallLibraryAttribute"/> so the Inspector
/// can match them against HOSTCALL usages in the script.
/// </summary>
public abstract class HostCallLibraryAsset : ScriptableObject
{
    public abstract HostCallLibrary GetLibrary();
}
