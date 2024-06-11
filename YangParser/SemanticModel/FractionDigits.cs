using System;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class FractionDigits : Statement
{
    public const string Keyword = "fraction-digits";

    public FractionDigits(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
    }
}