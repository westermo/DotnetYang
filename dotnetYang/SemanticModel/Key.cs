using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Key : Statement
{
    public Key(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        KeyFieldNames = SingleLine(Argument).Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    public const string Keyword = "key";

    /// <summary>
    /// The raw YANG key field names (e.g., ["name"] or ["resource", "alarm-type-id"]).
    /// </summary>
    public string[] KeyFieldNames { get; }

    /// <summary>
    /// The C#-ified key field names.
    /// </summary>
    public string[] KeyPropertyNames => KeyFieldNames.Select(MakeName).ToArray();

    public override string ToCode()
    {
        Parent?.Attributes.Add(
            $"Key({string.Join(", ", KeyFieldNames.Select(x => $"nameof({MakeName(x)})"))})");
        return string.Empty;
    }
}