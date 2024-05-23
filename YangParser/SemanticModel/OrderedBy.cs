using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class OrderedBy : Statement
{
    public OrderedBy(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

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

    public override string ToCode()
    {
        Parent?.Attributes.Add($"OrderedBy(\"{Argument}\")");
        return string.Empty;
    }
}