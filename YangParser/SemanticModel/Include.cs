using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Include : Statement
{
    public Include(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }
    public const string Keyword = "include";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(RevisionDate.Keyword),
    ];
}