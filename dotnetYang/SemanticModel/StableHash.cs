namespace YangParser.SemanticModel;

/// <summary>
/// Process-independent string hashing for names that end up in generated code.
/// <see cref="string.GetHashCode()"/> is randomized per process on .NET Core, so it must not be
/// used for anything emitted by the generator.
/// </summary>
public static class StableHash
{
    /// <summary>32-bit FNV-1a hash over the UTF-16 code units of <paramref name="value"/>.</summary>
    public static int Compute(string value)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in value)
            {
                hash = (hash ^ (byte)c) * 16777619u;
                hash = (hash ^ (byte)(c >> 8)) * 16777619u;
            }

            return (int)hash;
        }
    }
}
