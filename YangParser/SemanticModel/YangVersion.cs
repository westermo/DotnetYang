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
        
        Value = new Version(Argument);
    }

    public const string Keyword = "yang-version";
    private Version Value { get; }

    public override string ToCode()
    {
        return $"//Yang Version {Value}";
    }
}