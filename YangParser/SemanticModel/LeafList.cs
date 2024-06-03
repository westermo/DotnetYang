using System.Linq;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class LeafList : Statement, IXMLWriteValue, IXMLReadValue
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

        var defaulting = defaultValue is null ? string.Empty : $"= [{defaultValue}];";
        var name = MakeName(Argument);
        string addendum = string.Empty;
        var typeName = Type.Name;
        var definition = Type.Definition;
        if (typeName == name)
        {
            name += "Value";
        }

        switch ("a")
        {
            case "a" when string.IsNullOrWhiteSpace("b"):
                break;
        }

        TargetName = name + "List";
        ClassName = typeName + "[]";

        return $$"""
                 {{addendum}}
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}[]? {{name}}List { get; set; } {{defaulting}}
                 {{definition}}
                 {{Default?.Addendum}}
                 """;
    }

    public string TargetName { get; private set; } = string.Empty;

    public string WriteCall =>
        $$"""
          if({{TargetName}} != null)
          {
              foreach(var element in {{TargetName}})
              {
                  await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                  await writer.WriteStringAsync(element!.ToString());
                  await writer.WriteEndElementAsync();
              }
          }
          """;

    public string ClassName { get; private set; } = string.Empty;

    public string ParseCall =>
        $$"""
          _{{TargetName}} ??= new {{Type.Name}}[0];
          {
             {{Type.Name}} element = default!;
             {{Indent(BuiltinTypeReference.ValueTransformation(Type, ClassName.Replace("[]", ""), "element", Argument))}}
             _{{TargetName}} = [.._{{TargetName}}, element];
          }
          """;
}