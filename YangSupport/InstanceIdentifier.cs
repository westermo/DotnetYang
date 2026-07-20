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
    /// Resolves this instance-identifier against a Configuration root, returning
    /// the referenced object or null if the path cannot be resolved.
    /// </summary>
    /// <param name="root">The Configuration root that exposes a ResolveInstanceIdentifier method.</param>
    /// <returns>The resolved object, or null if not found.</returns>
    public object? Resolve(object root)
    {
        var method = root.GetType().GetMethod("ResolveInstanceIdentifier", new[] { typeof(string) });
        if (method is null) return null;
        return method.Invoke(root, new object[] { Path });
    }
}