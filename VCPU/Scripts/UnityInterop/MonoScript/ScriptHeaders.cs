/// <summary>
/// Directives parsed from the .Headers section of a .why script.
/// Fields default to zero/false so the runner can fall back to its own defaults.
/// </summary>
public class ScriptHeaders
{
    public Headers Format = Headers.NONE;
    public uint MemSize = 0;
    public uint StackSize = 0;
    public int    Entry = 0;
    public string EntryLabel = null;
    public bool Debug = false;
    public bool Strict = false;
    public int Timeout = 0;
    public int TickRate = 0;
    public bool DumpOnCrash = false;
    public bool NoHostCall = false;
    public int Loops = 0;
    public bool Profile = false;
    public bool DumpOnExit = false;
    public bool StackProtect = false;
}
