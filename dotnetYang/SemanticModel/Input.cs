using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Input : Statement, IXMLParseable
{
    public Input(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        ValidateChildren(statement);
        if (!string.IsNullOrWhiteSpace(Argument))
            throw new SemanticError($"{Keyword} statement may not have an argument", statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "input";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        Argument = Parent!.Argument;
        return $$"""
                 public class {{ClassName}}
                 {
                     {{string.Join("\n\t", Children.Select(child => Indent(child.ToCode())))}}
                     {{Indent(WriteFunctionInvisibleSelf())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    public string ClassName => $"{MakeName(Parent!.Argument)}Input";
    public string? TargetName => null;
}