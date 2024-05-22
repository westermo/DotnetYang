using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Grouping : Statement
{
    public Grouping(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "grouping";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        return string.Empty;
    }

    public IStatement[] WithUse(Uses use)
    {
        foreach (var inner in this.Unwrap().OfType<Uses>().ToArray())
        {
            inner.Expand();
        }

        return Children;
    }
}