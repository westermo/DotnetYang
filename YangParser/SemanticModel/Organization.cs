using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Organization : Statement
{
    public Organization(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }


    public const string Keyword = "organization";

    public override string ToCode()
    {
        return $"public const string Organization = \"{SingleLine(Argument)}\";";
    }
}