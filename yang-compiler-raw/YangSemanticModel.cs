using Microsoft.CodeAnalysis.Text;

namespace Yang.Compiler;

internal class YangSemanticModel
{
    public static YangSemanticModel Parse(SourceText text)
    {
        return new();
    }
}