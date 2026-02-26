using System.Linq;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class Leaf : Statement, IXMLWriteValue, IXMLReadValue
{
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Config.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
        new ChildRule(When.Keyword)
    ];

    public Leaf(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    private bool GetRequired() => Children.FirstOrDefault(child => child is Mandatory)?.Argument == "true";

    private DefaultValue? GetDefault() => Children.FirstOrDefault(child => child is DefaultValue) as DefaultValue;

    public const string Keyword = "leaf";
    private Type GetTypeChild() => Children.OfType<Type>().First();

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var currentDefault = GetDefault();
        var currentRequired = GetRequired();
        var currentType = GetTypeChild();

        if (currentRequired && currentDefault is not null)
        {
            throw new SemanticError(
                $"The '{DefaultValue.Keyword}' statement must not be present on nodes where '{Mandatory.Keyword}' is 'true'",
                Source);
        }

        var defaultValue = currentDefault?.ToCode();

        var defaulting = defaultValue is null ? string.Empty : $"= {defaultValue};";
        var nullable = currentRequired && !Children.Any(c => c is When) ? string.Empty : "?";
        var name = MakeName(Argument);
        var typeName = currentType.Name;
        var definition = currentType.Definition;
        if (typeName == name)
        {
            name += "Value";
        }

        TargetName = name;

        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}{{nullable}} {{name}} { get; set; } {{defaulting}}
                 {{definition}}
                 {{currentDefault?.Addendum}}
                 """;
    }

    public string TargetName { get; private set; } = string.Empty;

    public string WriteCall
    {
        get
        {
            var type = GetTypeChild();
            if (type.GetBaseType(out var prefix, out _) is "enumeration" or "bits" or "identityref")
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = type.Name!.Prefix(out _);
                }

                if (string.IsNullOrEmpty(prefix))
                {
                    if (BuiltinTypeReference.IsBuiltinKeyword(type.Argument) &&
                        type.Argument != "identityref") //Is direct subtype, identitys are always on top-level
                    {
                        return $$"""
                                 if({{TargetName}} != default)
                                 {
                                     await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                                     await writer.WriteStringAsync(GetEncodedValue({{TargetName}}!));
                                     await writer.WriteEndElementAsync();
                                 }
                                 """;
                    }

                    //Is local reference.
                    return $$"""
                             if({{TargetName}} != default)
                             {
                                 await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                                 await writer.WriteStringAsync(YangNode.GetEncodedValue({{TargetName}}!));
                                 await writer.WriteEndElementAsync();
                             }
                             """;
                }

                //Is imported reference
                var p = prefix.Contains('.') ? prefix : prefix + ":";
                return $$"""
                         if({{TargetName}} != default)
                         {
                             await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                             await writer.WriteStringAsync({{p}}GetEncodedValue({{TargetName}}!));
                             await writer.WriteEndElementAsync();
                         }
                         """;
            }

            if (type.GetBaseType(out _, out _) is "empty")
            {
                return $$"""
                         if({{TargetName}} != default)
                         {
                             await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                             await writer.WriteEndElementAsync();
                         }
                         """;
            }

            return $$"""
                     if({{TargetName}} != default)
                     {
                         await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                         await writer.WriteStringAsync({{TargetName}}!.ToString());
                         await writer.WriteEndElementAsync();
                     }
                     """;
        }
    }

    public string ClassName => GetTypeChild().Name!;

    public string ParseCall => BuiltinTypeReference.ValueTransformation(GetTypeChild(), ClassName, "_" + TargetName, Argument);
}