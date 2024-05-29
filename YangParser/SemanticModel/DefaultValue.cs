using System;
using System.Linq;
using System.Text.RegularExpressions;
using YangParser.Generator;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class DefaultValue : Statement
{
    public DefaultValue(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "default";

    public override string ToCode()
    {
        string prefix = Argument.Prefix(out var value);
        var type = Parent!.GetChild<Type>();
        return GetTypeSpecification(prefix, value, type);
    }

    private string GetTypeSpecification(string prefix, string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "null";
        }

        var aPrefix = string.IsNullOrEmpty(prefix) ? "" : prefix.Contains('.') ? prefix : prefix + ":";
        switch (type.Argument)
        {
            case "bits":
            case "enumeration":
                return aPrefix + type.Name + "." + MakeName(value);
            case "identityref":
                return $"\"{Argument}\"";
            case "string":
                return $"\"{Argument}\"";
            case "boolean":
                return Argument.ToLower();
            case "uint8":
            case "uint16":
            case "uint32":
            case "uint64":
            case "int8":
            case "int16":
            case "int32":
            case "int64":
            case "decimal64":
                return Argument;
            case "union":
                if (onlyNumbers.Match(Argument).Success)
                {
                    return $"new({Argument})";
                }

                var enumeration = type.SearchDownwards<Enum>(Argument);
                if (enumeration != null)
                {
                    return aPrefix + type.Name + "." + enumeration.Ancestor<Type>()!.Name + "." + MakeName(Argument);
                }

                var bit = type.SearchDownwards<Bit>(Argument);
                if (bit != null)
                {
                    return aPrefix + type.Name + "." + bit.Ancestor<Type>()!.Name + "." + MakeName(Argument);
                }

                foreach (var subType in type.Children.OfType<Type>())
                {
                    var possible = GetTypeSpecification(prefix, value, subType);
                    if (possible != $"new(\"{Argument}\")")
                    {
                        if (possible == $"\"{Argument}\"")
                        {
                            return $"new(\"{Argument}\")";
                        }

                        return possible == Argument ? $"new({Argument})" : possible;
                    }
                }

                Log.Write($"Fallback for 'default {Argument}' to type {type}");
                return $"new(\"{Argument}\")";
            default:
                var source = this.FindReference<TypeDefinition>(type.Argument);
                if (source is not null)
                {
                    if (string.IsNullOrEmpty(prefix))
                    {
                        prefix = type.Argument.Prefix(out _);
                    }

                    return GetTypeSpecification(prefix, value, source.GetChild<Type>());
                }

                if (onlyNumbers.Match(Argument).Success)
                {
                    return $"new({Argument})";
                }

                Log.Write($"Fallback for 'default {Argument}' to type {type}");
                return $"new(\"{Argument}\")";
        }
    }

    private static Regex onlyNumbers = new Regex(@"^[0-9\.]+$");
    public string? Addendum { get; private set; }
}