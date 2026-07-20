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
        if (Argument.Trim().Equals("unbounded", StringComparison.OrdinalIgnoreCase))
        {
            Value = int.MaxValue;
            IsUnbounded = true;
        }
        else
        {
            Value = int.Parse(Argument);
            IsUnbounded = false;
        }
    }

    public int Value { get; }
    
    /// <summary>
    /// True when max-elements was explicitly set to "unbounded" (effectively no limit).
    /// </summary>
    public bool IsUnbounded { get; }

    public override string ToCode()
    {
        if (!IsUnbounded)
            Parent?.Attributes.Add($"MaxElements({Value})");
        return string.Empty;
    }
}