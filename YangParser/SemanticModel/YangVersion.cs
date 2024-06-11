using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class YangVersion : Statement
{
    public YangVersion(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "yang-version";

    public override string ToCode()
    {
        return $"//Yang Version {Argument}";
    }
}