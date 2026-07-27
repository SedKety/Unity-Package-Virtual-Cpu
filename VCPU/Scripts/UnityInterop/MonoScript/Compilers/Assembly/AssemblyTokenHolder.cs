using UnityEngine;

[CreateAssetMenu(fileName = "AssemblyTokenHolder", menuName = "WHY/Assembly/Token Holder")]
public class AssemblyTokenHolder : ScriptableObject
{
    [SerializeField] private AssemblyToken[] _tokens;

    /// <summary>
    /// Tries retrieving the assembly token for the given string token. 
    /// Returns true if found, false otherwise.
    /// </summary>
    /// <param name="token">The mnemonical representation of the assembly instruction.</param>
    /// <param name="assemblyToken">The retrieved assembly token if found.</param>
    /// <returns>True if the token is found, false otherwise.</returns>
    public bool TryGetToken(string token, out AssemblyToken assemblyToken)
    {
        foreach (var t in _tokens)
        {
            if (t.Token.Equals(token, System.StringComparison.OrdinalIgnoreCase))
            {
                assemblyToken = t;
                return true;
            }
        }
        assemblyToken = default;
        return false;
    }
}
