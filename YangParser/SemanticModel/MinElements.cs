using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class MinElements : Statement
{
    public const string Keyword = "min-elements";

    public MinElements(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        Value = int.Parse(Argument);
    }
    public int Value { get; }
    public override string ToCode()
    {
        Parent?.Attributes.Add($"MinElements({Value})");
        return string.Empty;
    }
}