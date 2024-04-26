using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Status : Statement
{
    public Status(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        switch (Argument)
        {
            case "current":
            case "deprecated":
            case "obsolete":
                break;
            default:
                throw new InvalidOperationException($"Invalid {Keyword} value '{Argument}'");
        }
    }

    public const string Keyword = "status";
}
