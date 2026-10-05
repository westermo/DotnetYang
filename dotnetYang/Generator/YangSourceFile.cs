using System;

namespace YangParser.Generator;

/// <summary>
/// Value-equatable snapshot of a <c>.yang</c> additional file, used as incremental pipeline input.
/// </summary>
internal sealed class YangSourceFile : IEquatable<YangSourceFile>
{
    public YangSourceFile(string path, string? content)
    {
        Path = path;
        Content = content;
    }

    public string Path { get; }
    public string? Content { get; }

    public bool Equals(YangSourceFile? other) =>
        other is not null &&
        string.Equals(Path, other.Path, StringComparison.Ordinal) &&
        string.Equals(Content, other.Content, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as YangSourceFile);

    public override int GetHashCode()
    {
        unchecked
        {
            return (StringComparer.Ordinal.GetHashCode(Path) * 397) ^
                   (Content is null ? 0 : StringComparer.Ordinal.GetHashCode(Content));
        }
    }
}
