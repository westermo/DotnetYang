using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class When : Statement, IUnexpandable
{
    public When(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public const string Keyword = "when";

    public override string ToCode()
    {
        while (Argument.Contains("  "))
        {
            Argument = Argument.Replace("  ", " ");
        }

        Parent?.Attributes.Add($"When(\"{SingleLine(Argument).Replace("\"", "\\\"")}\")");
        return string.Empty;
    }
}