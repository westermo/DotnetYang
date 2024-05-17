using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Choice : Statement, IClassSource
{
    public Choice(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "choice";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Case.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(StateData.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(When.Keyword, Cardinality.ZeroOrMore)
    ];
}