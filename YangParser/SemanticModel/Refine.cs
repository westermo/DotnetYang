using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Refine : Statement
{
    public Refine(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "refine";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Presence.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MinElements.Keyword),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(StateData.Keyword)
    ];
}