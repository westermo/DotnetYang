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

        var defaulting = Default is null ? string.Empty : $"= {Default.ToCode()}";
        var nullable = Required ? string.Empty : "?";
        var name = MakeName(Argument);
        if (TypeName(Type) == "enum")
        {
            defaulting = Default is null ? string.Empty : $"= {name}Values.{MakeName(Default.ToCode())}";
            var enums = Type.Children.OfType<Enum>();
            var stringified = enums.Select(e =>
                e.Children.FirstOrDefault(c => c is Value) is Value value
                    ? $"{MakeName(e.Argument)} = {value.Argument}"
                    : $"{MakeName(e.Argument)}");
            return $$"""
                     public enum {{name}}Values
                     {
                         {{Indent(string.Join(",\n", stringified))}}
                     }
                     {{DescriptionString}}
                     {{AttributeString}}
                     public{{KeywordString}}{{name}}Values {{name}} { get; set; } {{defaulting}}
                     """;
        }

        return $$"""
                 {{DescriptionString}}
                 {{AttributeString}}
                 public{{KeywordString}}{{TypeName(Type)}}{{nullable}} {{name}} { get; set; } {{defaulting}}
                 """;
    }
}