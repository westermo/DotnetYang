using System;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class LeafList : Statement, IPropertySource
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
        new ChildRule(SemanticModel.Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(When.Keyword)
    ];

    public LeafList(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        Type = TypeName(Children.OfType<Type>().First());
        Default = Children.FirstOrDefault(child => child is DefaultValue) as DefaultValue;
    }

    private DefaultValue? Default { get; }

    public const string Keyword = "leaf-list";
    public string Type { get; }

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var defaulting = Default is null ? string.Empty : $"= {Default?.ToCode()}";
        return $$"""
                 {{DescriptionString}}
                 {{AttributeString}}
                 public{{KeywordString}}{{Type}}[]? {{MakeName(Argument)}} { get; set; } {{defaulting}}
                 """;
    }
}