using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Import : Statement
{
    public Import(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "import";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Prefix.Keyword, Cardinality.Required),
        new ChildRule(RevisionDate.Keyword),
    ];
}