using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Range : Statement
{
    public Range(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
    }
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Reference.Keyword),
    ];
    public const string Keyword = "range";
}
