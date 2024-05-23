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
        new ChildRule(SemanticModel.Type.Keyword, Cardinality.Required),
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
    public Type Type { get; }

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
        }
        // else if (Type.Argument is "identityref")
        // {
        //     defaulting = defaultValue is null ? string.Empty : $"= new {MakeName(defaultValue).Replace(";", "")}();";
        //     var components = MakeName(Type.GetChild<Base>().Argument).Split(':');
        //     components[components.Length - 1] = "I" + components[components.Length - 1];
        //
        //     typeName = string.Join(":", components);
        // }
        else if (defaultValue?.Contains('"') == false) //Is not a string 
        {
            if (!double.TryParse(defaultValue, out _)) //Is not a number
            {
                //Assume is enum;
                defaulting = $"= {typeName}.{MakeName(defaultValue)}";
            }
        }

        return $$"""
                 {{addendum}}
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}[]? {{MakeName(Argument)}} { get; set; } {{defaulting}}
                 """;
    }
}