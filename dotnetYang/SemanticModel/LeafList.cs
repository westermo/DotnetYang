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
    }

    private DefaultValue? GetDefault() => Children.FirstOrDefault(child => child is DefaultValue) as DefaultValue;

    public const string Keyword = "leaf-list";
    private Type GetTypeChild() => Children.OfType<Type>().First();

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        var currentDefault = GetDefault();
        var currentType = GetTypeChild();
        var defaultValue = currentDefault?.ToCode();

        var defaulting = defaultValue is null ? string.Empty : $"= [ {defaultValue} ];";
        var name = MakeName(Argument);
        string addendum = string.Empty;
        var typeName = currentType.Name;
        var definition = currentType.Definition;
        if (typeName == name)
        {
            name += "Value";
        }

        TargetName = name + "List";
        ClassName = typeName + "[]";

        var hasMinElements = this.TryGetChild<MinElements>(out var minEl) && minEl!.Value > 0;
        var nullable = hasMinElements && !Children.Any(c => c is When) ? string.Empty : "?";

        return $$"""
                 {{addendum}}
                 {{DescriptionString}}{{AttributeString}}
                 public{{KeywordString}}{{typeName}}[]{{nullable}} {{name}}List { get; set; } {{defaulting}}
                 {{definition}}
                 {{currentDefault?.Addendum}}
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

    public string ParseCall
    {
        get
        {
            var currentType = GetTypeChild();
            return $$"""
                     _{{TargetName}} ??= new {{currentType.Name}}[0];
                     {
                        {{currentType.Name}} element = default!;
                        {{Indent(BuiltinTypeReference.ValueTransformation(currentType, ClassName.Replace("[]", ""), "element", Argument))}}
                        _{{TargetName}} = [.._{{TargetName}}, element];
                     }
                     """;
        }
    }
}