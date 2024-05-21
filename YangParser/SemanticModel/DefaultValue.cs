using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class DefaultValue : Statement
{
    public DefaultValue(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "default";

    public override string ToCode()
    {
        return Argument + ";";
    }
}