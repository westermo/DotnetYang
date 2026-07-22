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
        if (root is not IYangNode rootNode) return null;

        // Delegate to the root's ResolveInstanceIdentifier if it has one (Configuration class)
        var method = root.GetType().GetMethod("ResolveInstanceIdentifier", new[] { typeof(string) });
        if (method is not null)
        {
            return method.Invoke(root, new object[] { path });
        }

        // Fallback: walk the tree via GetChild
        return ResolveFromNode(rootNode, path);
    }

    private static object? ResolveFromNode(IYangNode root, string path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/') return null;
        var segments = path.Substring(1).Split('/');
        object? current = root;

        foreach (var segment in segments)
        {
            if (current is not IYangNode yangNode) return null;

            var bracketIdx = segment.IndexOf('[');
            var name = bracketIdx >= 0 ? segment.Substring(0, bracketIdx) : segment;

            // Strip module prefix
            var colonIdx = name.IndexOf(':');
            var localName = colonIdx >= 0 ? name.Substring(colonIdx + 1) : name;

            current = yangNode.GetChild(localName) ?? yangNode.GetChild(name);
            if (current is null) return null;

            // Handle key predicate for list access
            if (bracketIdx >= 0)
            {
                var predicate = segment.Substring(bracketIdx);
                var eqIdx = predicate.IndexOf('=');
                if (eqIdx > 0)
                {
                    var keyValue = predicate.Substring(eqIdx + 1).Trim('[', ']', '\'', '"', ' ');
                    var indexer = current.GetType().GetProperty("Item", new[] { typeof(string) });
                    if (indexer is not null)
                    {
                        try { current = indexer.GetValue(current, new object[] { keyValue }); }
                        catch { return null; }
                    }
                }
            }
        }
        return current;
    }
}
