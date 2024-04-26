using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Enum : Statement
{
    public Enum(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "enum";
    public override ChildRule[] PermittedChildren { get; } =
    {
        new ChildRule(Value.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
    };
}
