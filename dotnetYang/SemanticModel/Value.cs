using System;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Value : Statement
{
    public Value(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        Integer = int.Parse(Argument);
    }

    public const string Keyword = "value";

    public int Integer { get; }
}