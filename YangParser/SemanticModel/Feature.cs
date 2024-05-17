using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Feature : Statement
{
    public Feature(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Status.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public const string Keyword = "feature";
}