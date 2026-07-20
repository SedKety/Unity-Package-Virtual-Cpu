using UnityEngine;

/// <summary>
/// <see cref="ILogger"/> implementation that forwards VCPU output to both
/// the Unity Console and a <see cref="WhyTerminalWindow"/>.
/// </summary>
/// <remarks>
/// Pass an instance of this class to the <c>VCPU</c> constructor when running a script
/// from the editor. The terminal window must already be open or obtained via
/// <see cref="WhyTerminalWindow.GetOrOpen"/> before creating this logger.
/// </remarks>
public class WhyTerminalLogger : ILogger
{
    private readonly WhyTerminalWindow _terminal;

    /// <summary>
    /// Creates a logger that writes to the given terminal window.
    /// </summary>
    /// <param name="terminal">The terminal window to receive output.</param>
    public WhyTerminalLogger(WhyTerminalWindow terminal) => _terminal = terminal;

    /// <summary>
    /// Logs a message to the Unity Console (<see cref="Debug.Log"/>) and appends it
    /// to the terminal window as a new entry.
    /// </summary>
    /// <param name="message">The message produced by the VCPU (e.g. from SysWrite).</param>
    public void Log(string message)
    {
        Debug.Log(message);
        _terminal?.Append(message);
    }
}
