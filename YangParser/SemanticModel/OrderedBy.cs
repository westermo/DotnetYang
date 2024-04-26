using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class OrderedBy : Statement
{
    public OrderedBy(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        switch (Argument)
        {
            case "user":
            case "system":
                break;
            default:
                throw new InvalidOperationException($"Invalid value '{Argument}' for {Keyword} statement");
        }
    }

    public const string Keyword = "ordered-by";
}
