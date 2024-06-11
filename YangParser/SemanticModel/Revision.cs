using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Revision : Statement
{
    public Revision(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        Value = DateTime.Parse(Argument);
    }

    public const string Keyword = "revision";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public DateTime Value { get; }

    public override string ToCode()
    {
        return string.Empty;
    }
}