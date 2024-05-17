using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class ErrorAppTag : Statement
{
    public ErrorAppTag(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "error-app-tag";
}
