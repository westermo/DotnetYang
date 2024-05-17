using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Notification : Statement
{
    public Notification(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "notification";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore)
    ];
}