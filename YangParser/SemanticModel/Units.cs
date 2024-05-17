using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Units : Statement
{
    public Units(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
    }

    public const string Keyword = "units";
}