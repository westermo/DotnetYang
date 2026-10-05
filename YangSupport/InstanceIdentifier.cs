namespace YangSupport;

/// <summary>
/// Represents a YANG instance-identifier value — an XPath-like path that
/// identifies a specific node instance in the data tree.
/// </summary>
public class InstanceIdentifier(string path)
{
    public string Path { get; } = path;
    public static InstanceIdentifier Parse(string id) => new(id);
    public override string ToString() => Path;

    /// <summary>
    /// Resolves this instance-identifier against a YANG node, walking up to the root
    /// and resolving the path from there.
    /// </summary>
    public object? Resolve(IYangNode node)
    {
        return node.ResolveInstanceIdentifier(Path);
    }

    /// <summary>
    /// Resolves this instance-identifier against a root object (e.g. Configuration).
    /// </summary>
    public object? Resolve(object root) => root switch
    {
        IYangInstanceIdentifierRoot resolver => resolver.ResolveInstanceIdentifier(Path),
        IYangNode yangNode => yangNode.ResolveInstanceIdentifier(Path),
        _ => null
    };
}