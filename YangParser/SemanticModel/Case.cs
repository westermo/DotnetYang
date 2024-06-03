using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Case : Statement, IClassSource, IXMLParseable
{
    public List<string> Comments { get; } = new();

    public Case(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "case";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        var nodes = Children.Select(c => c.ToCode()).ToArray();
        return $$"""
                 public {{ClassName}}? {{TargetName}};
                 {{DescriptionString}}{{AttributeString}}
                 public class {{ClassName}}
                 {
                    {{Indent(string.Join("\n", nodes))}}
                     {{Indent(WriteFunctionInvisibleSelf())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument) + "CaseValue";

    public IEnumerable<string> SubTargets =>
        Children.Where(c => c is IXMLParseable or IXMLReadValue).Select(c => c.Argument).Distinct();

    public string ClassName => TargetName + "Case";
}