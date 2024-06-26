using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Length : Statement
{
    public Length(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        foreach (var entry in Argument.Split('|'))
        {
            Bounds.Add(GetLength(entry));
        }
    }

    private (int, int) GetLength(string entry)
    {
        if (entry.Contains(".."))
        {
            var components = entry.Replace("..", "|").Split('|');
            var lower = int.Parse(components[0]);
            var upper = components[1] == "max" ? int.MaxValue : int.Parse(components[1]);
            return (lower, upper);
        }

        var value = int.Parse(entry);
        return (value, value);
    }

    public string GetConstructorValidation()
    {
        var qualifiers = Bounds.Select(bound => $"input.Length is >= {bound.Item1} and <= {bound.Item2}");
        var all = string.Join("||", qualifiers);
        var hasError = this.TryGetChild<ErrorMessage>(out var errorMessage);
        var hasTag = this.TryGetChild<ErrorAppTag>(out var appTag);
        var message = hasTag || hasError
            ? $"\"{SingleLine(appTag?.Argument ?? "No tag")}: {SingleLine(errorMessage?.Argument ?? string.Empty)}\""
            : $"$\"string \\\"{{input}}\\\" does not match qualification '{all}'\"";
        return $"if(!({all})) throw new ArgumentException({message});";
    }


    public List<(int, int)> Bounds { get; } = new();

    public const string Keyword = "length";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Reference.Keyword),
    ];
}