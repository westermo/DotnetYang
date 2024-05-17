using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class AnyXml : Statement
{
    public AnyXml(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }
    public const string Keyword = "anyxml";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(StateData.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(When.Keyword)
    ];
}
