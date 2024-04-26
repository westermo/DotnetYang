using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class MinElements : Statement
{
    public const string Keyword = "min-elements";

    public MinElements(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Value = int.Parse(Argument);
    }
    public int Value { get; }
}