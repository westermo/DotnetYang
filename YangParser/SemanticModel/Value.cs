using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Value : Statement
{
    public Value(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Integer = int.Parse(Argument);
    }

    public const string Keyword = "value";

    public int Integer { get; }
}