using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Leaf : Statement
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

    public bool Required { get; }

    private DefaultValue? Default { get; }

    public const string Keyword = "leaf";
    public Type Type { get; }

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var defaultValue = Default?.ToCode();

        var defaulting = defaultValue is null ? string.Empty : $"= {defaultValue}";
        var nullable = Required ? string.Empty : "?";
        var name = MakeName(Argument);
        var typeName = TypeName(Type);
        string addendum = string.Empty;
        if (Type.Argument is "enumeration" or "union" or "leafref" or "instance-identifier" or "bits" or "leafref")
        {
            typeName = $"{name}Definition";
            addendum = HandleType(Type, typeName);
        }

        if (Type.Argument is "enumeration" or "bits")
        {
            defaulting = defaultValue is null ? string.Empty : $"= {typeName}.{MakeName(defaultValue)}";
        }
        else if (Type.Argument.Contains("identityref"))
        {
            var ifaceName = InterfaceName(Type.GetChild<Base>());
            if (ifaceName.Contains(':'))
            {
                typeName = ifaceName;
            }
            else
            {
                typeName = typeName.Replace("Identityref", ifaceName);
            }

            if (Default is not null)
            {
                var className = MakeName(defaultValue!).Replace(";", "");
                defaulting = $"= new {className.Split(':').Last()}Impl();";
                addendum = $"public class {className.Split(':').Last()}Impl : {InterfaceName(className)};";
            }
        }
        else if (Type.Argument is "string")
        {
            defaulting = Default is null ? string.Empty : $"= \"{Default?.Argument}\";";
        }
        else if (Type.Argument is "boolean")
        {
            defaulting = Default is null ? string.Empty : $"= {Default?.Argument.ToLower()};";
        }
        else if (Type.Argument is "union")
        {
            if (Default is not null)
            {
                foreach (var e in Type.Unwrap().OfType<Enum>())
                {
                    if (e.Argument == Default.Argument)
                    {
                        defaulting =
                            $"= {name}Union{Array.IndexOf(e.Parent!.Parent!.Children, e.Parent)}.{MakeName(e.Argument)};";
                    }
                }
            }
        }
        else if (defaultValue?.Contains('"') == false) //Is not a string 
        {
            if (!double.TryParse(defaultValue.Replace(";", ""), out _)) //Is not a number
            {
                //Assume is enum;
                defaulting = $"= {typeName}.{MakeName(defaultValue)}";
            }
        }

        return $$"""
                 //{{Type.Argument}}
                 {{addendum}}
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}{{nullable}} {{name}} { get; set; } {{defaulting}}
                 """;
    }
}