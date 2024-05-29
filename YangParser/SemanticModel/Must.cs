using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Must : Statement
{
    public Must(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "must";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword)
    ];

    public override string ToCode()
    {
        Parent?.Attributes.Add($"Must(\"{SingleLine(Argument).Replace("\"", "\\\"")}\")");
        return string.Empty;
    }
}