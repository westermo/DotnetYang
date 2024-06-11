using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class MaxElements : Statement
{
    public const string Keyword = "max-elements";

    public MaxElements(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        Value = int.Parse(Argument);
    }

    public int Value { get; }

    public override string ToCode()
    {
        Parent?.Attributes.Add($"MaxElements({Value})");
        return string.Empty;
    }
}