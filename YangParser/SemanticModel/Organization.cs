using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Organization : Statement
{
    public Organization(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }


    public const string Keyword = "organization";
}
