using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Deviation : Statement
{
    public Deviation(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }
    public const string Keyword = "deviation";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Deviate.Keyword, Cardinality.OneOrMore),
        new ChildRule(Reference.Keyword),
    ];
}
