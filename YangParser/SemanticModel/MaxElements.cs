using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class MaxElements : Statement
{
    public const string Keyword = "max-elements";

    public MaxElements(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Value = int.Parse(Argument);
    }
    public int Value { get; }
}
