using System;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class LeafList : Statement
{
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Config.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(MinElements.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(OrderedBy.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(When.Keyword)
    ];

    public LeafList(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        Type = Children.OfType<Type>().First();
        Default = Children.FirstOrDefault(child => child is DefaultValue) as DefaultValue;
    }

    private DefaultValue? Default { get; }

    public const string Keyword = "leaf-list";
    private Type Type { get; }

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var defaultValue = Default?.ToCode();

        var defaulting = defaultValue is null ? string.Empty : $"= {defaultValue}";
        var name = MakeName(Argument);
        var typeName = TypeName(Type);
        string addendum = string.Empty;
        return $$"""
                 {{addendum}}
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{Type.Name}}[]? {{name}} { get; set; } {{defaulting}}
                 {{Type.Definition}}
                 {{Default?.Addendum}}
                 """;
    }
}