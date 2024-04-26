using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Deviate : Statement
{
    public Deviate(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        switch (Argument)
        {
            case "not-supported":
            case "add":
            case "replace":
            case "delete":
                break;
            default:
                throw new InvalidOperationException($"Invalid argument '{Argument}' for keyword '{Keyword}'");
        }
    }
    public const string Keyword = "deviate";
}