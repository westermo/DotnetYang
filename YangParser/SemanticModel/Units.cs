using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Units : Statement
{
    public Units(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
    }

    public const string Keyword = "units";
}