using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

/// <summary>
/// Shared implementation of the 'input' and 'output' statements of RPCs and actions.
/// </summary>
public abstract class OperationDataStatement : Statement, IXMLParseable
{
    private readonly string _keyword;

    protected OperationDataStatement(YangStatement statement, string keyword) : base(statement)
    {
        _keyword = keyword;
        if (statement.Keyword != keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {keyword}", statement);
        ValidateChildren(statement);
        if (!string.IsNullOrWhiteSpace(Argument))
            throw new SemanticError($"{keyword} statement may not have an argument", statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    /// <summary>Name of the XML element wrapping this data when parsed.</summary>
    protected abstract string XmlElementName { get; }

    public override string ToCode()
    {
        Argument = XmlElementName;
        return $$"""
                 public class {{ClassName}} : YangSupport.IYangNode
                 {
                     YangSupport.IYangNode? YangSupport.IYangNode.YangParent => null;
                     public object? GetChild(string yangName) => null;
                     {{string.Join("\n\t", Children.Select(child => Indent(child.ToCode())))}}
                     {{Indent(WriteFunctionInvisibleSelf())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    public string ClassName => $"{MakeName(Parent!.Argument)}{char.ToUpperInvariant(_keyword[0])}{_keyword.Substring(1)}";
    public string? TargetName => null;
}
