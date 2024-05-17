using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class RevisionDate : Statement
{
    public RevisionDate(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        if (!DateTime.TryParse(Argument, out _)) throw new InvalidOperationException($"Invalid date format '{Argument}'");
    }

    public const string Keyword = "revision-date";
}
