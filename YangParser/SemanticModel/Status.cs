using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Status : Statement, IAttributeSource
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
                break;
            case "deprecated":
            case "obsolete":
                Active = true;
                break;
            default:
                throw new InvalidOperationException($"Invalid {Keyword} value '{Argument}'");
        }
    }

    public const string Keyword = "status";
    public string AttributeName => "Obsolete";
    public bool Active { get; }
}