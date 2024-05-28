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
    override public string ToCode()
    {
        Parent?.Attributes.Add("Inherits(\"" + Argument + "\")");
        return string.Empty;
    }
}
