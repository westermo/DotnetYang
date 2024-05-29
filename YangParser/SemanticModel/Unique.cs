using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Unique : Statement
{
    public Unique(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        ValidateChildren(statement);
        Identifiers = Argument.Split(' ', '\n', '\t');
    }

    public const string Keyword = "unique";

    public string[] Identifiers { get; }

    public override string ToCode()
    {
        Parent?.Attributes.Add(
            $"Unique({string.Join(",", Identifiers.Select(i => $"nameof({MakeName(SingleLine(i).Replace("\"", "\\\""))})"))})");
        return string.Empty;
    }
}