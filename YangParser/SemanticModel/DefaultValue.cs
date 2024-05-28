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
        switch (type.Argument)
        {
            case "bits":
            case "enumeration":
                return prefix + type.Name + "." + MakeName(value);
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
                var enumeration = Parent!.GetChild<Type>().SearchDownwards<Enum>(Argument);
                if (enumeration != null)
                {
                    return prefix + Parent!.GetChild<Type>().Name + "." + enumeration.Ancestor<Type>()!.Name + "." + MakeName(Argument);
                }
                return $"new(\"{Argument}\")";
            default:
                var source = this.FindReference<TypeDefinition>(type.Argument);
                if (source is not null)
                {
                    return GetTypeSpecification(prefix, value, source.GetChild<Type>());
                }
                if (onlyNumbers.Match(Argument).Success)
                {
                    return $"new({Argument})";
                }
                return $"new(\"{Argument}\")";
        }
    }

    private static Regex onlyNumbers = new Regex(@"^[0-9\.]+$");
    public string? Addendum { get; private set; }
}