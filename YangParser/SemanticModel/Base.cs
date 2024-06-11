using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Base : Statement, IUnexpandable
{
    public Base(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

    }
    public const string Keyword = "base";
    public override string ToCode()
    {
        return string.Empty;
    }
}
