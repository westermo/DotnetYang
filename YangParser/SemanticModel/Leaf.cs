using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Leaf : Statement, IXMLValue
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

        Type = Children.OfType<Type>().First();

        Default = Children.FirstOrDefault(child => child is DefaultValue) as DefaultValue;
        Required = Children.FirstOrDefault(child => child is Mandatory)?.Argument == "true";
        if (Required && Default is not null)
        {
            throw new SemanticError(
                $"The '{DefaultValue.Keyword}' statement must not be present on nodes where '{Mandatory.Keyword}' is 'true'",
                statement);
        }
    }

    private bool Required { get; }

    private DefaultValue? Default { get; }

    public const string Keyword = "leaf";
    private Type Type { get; }

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var defaultValue = Default?.ToCode();

        var defaulting = defaultValue is null ? string.Empty : $"= {defaultValue};";
        var nullable = Required ? string.Empty : "?";
        var name = MakeName(Argument);
        var typeName = Type.Name;
        var definition = Type.Definition;
        if (typeName == name)
        {
            name += "Value";
        }

        TargetName = name;

        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}{{nullable}} {{name}} { get; set; } {{defaulting}}
                 {{definition}}
                 {{Default?.Addendum}}
                 """;
    }

    public string TargetName { get; private set; } = string.Empty;

    public string WriteCall
    {
        get
        {
            var pre = string.IsNullOrWhiteSpace(Prefix) ? "null" : $"\"{Prefix}\"";
            if (Type.Argument is "enumeration" or "bits")
            {
                return $$"""
                         if({{TargetName}} != default)
                         {
                             await writer.WriteStartElementAsync({{pre}},"{{Argument}}",null);
                             await writer.WriteStringAsync(GetEncodedValue({{TargetName}}!));
                             await writer.WriteEndElementAsync();
                         }
                         """;
            }

            return $$"""
                     if({{TargetName}} != default)
                     {
                         await writer.WriteStartElementAsync({{pre}},"{{Argument}}",null);
                         await writer.WriteStringAsync({{TargetName}}!.ToString());
                         await writer.WriteEndElementAsync();
                     }
                     """;
        }
    }
}