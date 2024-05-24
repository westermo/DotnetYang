using System;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

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
        var components = Argument.Split(':');
        string prefix = string.Empty;
        string value = components.Last();
        if (components.Length > 1)
        {
            prefix = components[0] + ":";
        }

        var source = this.FindReference(Argument);
        if (prefix == string.Empty)
        {
            prefix = source?.GetInheritedPrefix() + ":";
        }

        // Log.Write($"Default Value Location for '{Argument}': {source?.GetType()} {source?.Argument}, prefix: {prefix}");

        switch (source)
        {
            case Enum @enum:
                return prefix + ((Type)@enum!.Parent!).Name + "." + MakeName(value);
            case Identity:
                var localName = MakeName(Argument);
                Addendum = $"public class {localName.Split(':').Last()}Implementation : {InterfaceName(localName)};";
                return "new " + prefix + localName.Split(':').Last() + "Implementation()";
            default:
                return Parent!.Argument switch
                {
                    "string" => $"\"{Argument}\"",
                    "boolean" => Argument.ToLower(),
                    _ => Argument
                };
        }
    }

    public string? Addendum { get; private set; }
}