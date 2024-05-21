using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class ErrorAppTag : Statement
{
    public ErrorAppTag(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "error-app-tag";
}
