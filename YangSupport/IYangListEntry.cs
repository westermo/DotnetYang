namespace YangSupport;

/// <summary>
/// Implemented by generated entries of keyed YANG lists. Allows instance-identifier
/// key predicates (e.g. <c>list[a='x'][b='1']</c>) to be matched without reflection.
/// </summary>
public interface IYangListEntry
{
    /// <summary>
    /// Returns true when every key leaf of this entry has the given lexical value.
    /// </summary>
    /// <param name="keys">Key values indexed by local (unprefixed) YANG key leaf name.</param>
    bool YangMatchesKeys(IReadOnlyDictionary<string, string> keys);
}
