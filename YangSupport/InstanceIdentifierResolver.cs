using System.Collections;
using System.Globalization;
using System.Text;

namespace YangSupport;

/// <summary>
/// Reflection-free resolution of RFC 7950 §9.13 instance-identifier paths over a tree of
/// <see cref="IYangNode"/> instances. Keyed list entries are matched through
/// <see cref="IYangListEntry"/>; positional (<c>[n]</c>) and leaf-list value (<c>[.='v']</c>)
/// predicates are supported as well.
/// </summary>
public static class InstanceIdentifierResolver
{
    /// <summary>
    /// Resolves <paramref name="path"/> starting from <paramref name="root"/>.
    /// Returns the addressed object (node, list entry, leaf value) or null when the path
    /// is malformed or does not match any data in the tree.
    /// </summary>
    public static object? Resolve(IYangNode root, string path)
    {
        if (root is null || string.IsNullOrEmpty(path) || path[0] != '/') return null;

        var segments = SplitSegments(path, 1);
        if (segments is null) return null;

        object? current = root;
        foreach (var segment in segments)
        {
            if (current is not IYangNode node) return null;
            if (!TryParseSegment(segment, out var prefix, out var localName, out var predicates)) return null;

            current = Step(node, prefix, localName);
            if (current is null) return null;

            foreach (var predicate in predicates)
            {
                current = ApplyPredicate(current, predicate);
                if (current is null) return null;
            }
        }
        return current;
    }

    private static object? Step(IYangNode node, string? prefix, string localName)
    {
        var child = node.GetChild(localName);
        if (child is not null || prefix is null) return child;

        child = node.GetChild(prefix + ":" + localName);
        if (child is not null) return child;

        // A configuration root exposes module nodes by module name; hop through it.
        return node.GetChild(prefix) is IYangNode module ? module.GetChild(localName) : null;
    }

    private abstract class Predicate;

    private sealed class KeyPredicate(Dictionary<string, string> keys) : Predicate
    {
        public Dictionary<string, string> Keys { get; } = keys;
    }

    private sealed class PositionPredicate(int position) : Predicate
    {
        public int Position { get; } = position;
    }

    private sealed class LeafListPredicate(string value) : Predicate
    {
        public string Value { get; } = value;
    }

    private static object? ApplyPredicate(object current, Predicate predicate)
    {
        if (current is string || current is not IEnumerable items) return null;

        switch (predicate)
        {
            case KeyPredicate key:
                foreach (var item in items)
                {
                    if (item is IYangListEntry entry && entry.YangMatchesKeys(key.Keys)) return item;
                }
                return null;
            case PositionPredicate position:
                var index = 1;
                foreach (var item in items)
                {
                    if (index++ == position.Position) return item;
                }
                return null;
            case LeafListPredicate leafList:
                foreach (var item in items)
                {
                    if (item is not null && LexicalValue(item) == leafList.Value) return item;
                }
                return null;
            default:
                return null;
        }
    }

    private static string? LexicalValue(object value) => value switch
    {
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static List<string>? SplitSegments(string path, int start)
    {
        var segments = new List<string>();
        var builder = new StringBuilder();
        char quote = '\0';
        var depth = 0;
        for (var i = start; i < path.Length; i++)
        {
            var c = path[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (c is '\'' or '"' && depth > 0)
            {
                quote = c;
            }
            else if (c == '[')
            {
                depth++;
            }
            else if (c == ']')
            {
                depth--;
                if (depth < 0) return null;
            }
            else if (c == '/' && depth == 0)
            {
                segments.Add(builder.ToString());
                builder.Clear();
                continue;
            }
            builder.Append(c);
        }
        if (quote != '\0' || depth != 0) return null;
        segments.Add(builder.ToString());
        return segments;
    }

    private static bool TryParseSegment(string segment, out string? prefix, out string localName,
        out List<Predicate> predicates)
    {
        prefix = null;
        localName = string.Empty;
        predicates = new List<Predicate>();

        var bracket = segment.IndexOf('[');
        var name = (bracket >= 0 ? segment.Substring(0, bracket) : segment).Trim();
        if (name.Length == 0) return false;
        SplitPrefix(name, out prefix, out localName);
        if (bracket < 0) return true;

        Dictionary<string, string>? keys = null;
        var i = bracket;
        while (i < segment.Length)
        {
            while (i < segment.Length && char.IsWhiteSpace(segment[i])) i++;
            if (i >= segment.Length) break;
            if (segment[i] != '[') return false;
            if (!TryReadPredicate(segment, ref i, out var body)) return false;

            var eq = IndexOfUnquoted(body, '=');
            if (eq < 0)
            {
                if (!int.TryParse(body.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var position)
                    || position < 1) return false;
                predicates.Add(new PositionPredicate(position));
                continue;
            }

            var keyName = body.Substring(0, eq).Trim();
            if (!TryUnquote(body.Substring(eq + 1).Trim(), out var value)) return false;
            if (keyName == ".")
            {
                predicates.Add(new LeafListPredicate(value));
                continue;
            }

            SplitPrefix(keyName, out _, out var localKey);
            if (localKey.Length == 0) return false;
            if (keys is null)
            {
                keys = new Dictionary<string, string>(StringComparer.Ordinal);
                predicates.Add(new KeyPredicate(keys));
            }
            keys[localKey] = value;
        }
        return true;
    }

    private static bool TryReadPredicate(string segment, ref int i, out string body)
    {
        var start = ++i;
        char quote = '\0';
        for (; i < segment.Length; i++)
        {
            var c = segment[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == ']')
            {
                body = segment.Substring(start, i - start);
                i++;
                return true;
            }
        }
        body = string.Empty;
        return false;
    }

    private static int IndexOfUnquoted(string text, char target)
    {
        char quote = '\0';
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
            }
            else if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == target)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool TryUnquote(string text, out string value)
    {
        if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[text.Length - 1] == text[0])
        {
            value = text.Substring(1, text.Length - 2);
            return true;
        }
        value = string.Empty;
        return false;
    }

    private static void SplitPrefix(string name, out string? prefix, out string localName)
    {
        var colon = name.IndexOf(':');
        prefix = colon >= 0 ? name.Substring(0, colon) : null;
        localName = colon >= 0 ? name.Substring(colon + 1) : name;
    }
}
