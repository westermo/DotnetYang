using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Pattern : Statement
{
    public Pattern(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "pattern";
    public override ChildRule[] PermittedChildren { get; }
    = [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Reference.Keyword)
    ];
}