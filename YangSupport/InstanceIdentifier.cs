using System.Reflection;

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
    /// Falls back to reflection if the root doesn't implement IYangNode.
    /// </summary>
    public object? Resolve(object root)
    {
        if (root is IYangNode yangNode)
        {
            return yangNode.ResolveInstanceIdentifier(Path);
        }
        var method = root.GetType().GetMethod("ResolveInstanceIdentifier", new[] { typeof(string) });
        if (method is null) return null;
        return method.Invoke(root, new object[] { Path });
    }
}