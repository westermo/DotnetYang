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
        Identifiers = Argument.Split(' ', '\n', '\t').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    public const string Keyword = "unique";

    /// <summary>
    /// The raw YANG unique field identifiers.
    /// </summary>
    public string[] Identifiers { get; }

    /// <summary>
    /// The C# property names for the unique fields.
    /// </summary>
    public string[] PropertyNames => Identifiers.Select(i => MakeName(SingleLine(i).Replace("\"", ""))).ToArray();

    public override string ToCode()
    {
        Parent?.Attributes.Add(
            $"Unique({string.Join(",", Identifiers.Select(i => $"nameof({MakeName(SingleLine(i).Replace("\"", "\\\""))})"))})");
        return string.Empty;
    }
}