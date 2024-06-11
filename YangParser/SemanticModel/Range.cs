using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Range : Statement
{
    public Range(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        ValidateChildren(statement);
        foreach (var entry in Argument.Split('|'))
        {
            Bounds.Add(GetRange(entry));
        }
    }

    private (double, double) GetRange(string entry)
    {
        if (entry.Contains(".."))
        {
            var components = entry.Replace("..", "|").Split('|');
            var lower = double.Parse(components[0]);
            var upper = components[1] == "max" ? double.NaN : double.Parse(components[1]);
            return (lower, upper);
        }

        var value = double.Parse(entry);
        return (value, value);
    }

    public string GetConstructorValidation()
    {
        foreach (var bound in Bounds.ToArray())
        {
            var max = Parent!.Argument switch
            {
                "int8" => sbyte.MaxValue,
                "uint8" => byte.MaxValue,
                "int16" => short.MaxValue,
                "uint16" => ushort.MaxValue,
                "int32" => int.MaxValue,
                "uint32" => uint.MaxValue,
                "int64" => long.MaxValue,
                "uint64" => ulong.MaxValue,
                "decimal64" => double.MaxValue,
                _ => 0,
            };
            var min = Parent!.Argument switch
            {
                "int8" => sbyte.MinValue,
                "uint8" => byte.MinValue,
                "int16" => short.MinValue,
                "uint16" => ushort.MinValue,
                "int32" => int.MinValue,
                "uint32" => uint.MinValue,
                "int64" => long.MinValue,
                "uint64" => ulong.MinValue,
                "decimal64" => double.MinValue,
                _ => 0,
            };
            if (bound.Item2 >= max && bound.Item1 <= min)
            {
                Bounds.Remove(bound);
            }
        }

        if (Bounds.Count == 0)
        {
            return string.Empty;
        }

        var qualifiers = Bounds.Select(bound =>
            double.IsNaN(bound.Item2) ? $"input >= {bound.Item1}" : $"input is >= {bound.Item1} and <= {bound.Item2}");
        var all = string.Join("||", qualifiers);
        var hasError = this.TryGetChild<ErrorMessage>(out var errorMessage);
        var hasTag = this.TryGetChild<ErrorAppTag>(out var appTag);
        var message = hasTag || hasError
            ? $"\"{SingleLine(appTag?.Argument ?? "No tag")}: {SingleLine(errorMessage?.Argument ?? string.Empty)}\""
            : $"$\"string {{input}} does not match qualification '{all}'\"";
        return $"if(!({all})) throw new ArgumentException({message});";
    }

    public List<(double, double)> Bounds { get; } = new();

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Reference.Keyword),
    ];

    public const string Keyword = "range";
}