namespace YangSupport;

/// <summary>
/// Interface implemented by all generated YANG node classes (containers, list entries,
/// choices, cases, and module YangNode classes). Provides tree navigation without reflection.
/// </summary>
public interface IYangNode
{
    /// <summary>
    /// The parent node in the YANG data tree, or null if this is the root.
    /// </summary>
    IYangNode? YangParent { get; }

    /// <summary>
    /// Resolves a direct child by its YANG schema name.
    /// Returns the property value for the named child, or null if not found.
    /// </summary>
    object? GetChild(string yangName);
}

/// <summary>
/// Extension methods for IYangNode tree navigation.
/// </summary>
public static class YangNodeExtensions
{
    /// <summary>
    /// Walks up the YangParent chain to find the root node (the node whose YangParent is null).
    /// </summary>
    public static IYangNode GetRoot(this IYangNode node)
    {
        var current = node;
        while (current.YangParent is not null)
        {
            current = current.YangParent;
        }
        return current;
    }

    /// <summary>
    /// Resolves an instance-identifier path starting from the root of the tree.
    /// Path format: /module-name:container/child/list[key='value']/leaf
    /// </summary>
    public static object? ResolveInstanceIdentifier(this IYangNode node, string path)
    {
        var root = node.GetRoot();

        if (root is IYangInstanceIdentifierRoot resolver)
        {
            return resolver.ResolveInstanceIdentifier(path);
        }

        // Fallback: walk the tree via GetChild
        return InstanceIdentifierResolver.Resolve(root, path);
    }
}
