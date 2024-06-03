using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class List : Statement, IClassSource, IXMLWriteValue, IXMLReadValue
{
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Action.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Config.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Key.Keyword),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(MinElements.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(OrderedBy.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Unique.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword)
    ];

    public List(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "list";
    public List<string> Comments { get; } = new();


    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        string property =
            $"\n{DescriptionString}\npublic{KeywordString}List<{ClassName}>? {TargetName} {{ get; set; }}";
        return $$"""
                 {{property}}
                 {{AttributeString}}
                 public class {{ClassName}}
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);

    public string ClassName => TargetName + "Entry";

    public string WriteCall =>
        $$"""
          if({{TargetName}} != null)
          {
              foreach(var element in {{TargetName}})
              {
                  await element!.WriteXMLAsync(writer);
              }
          }
          """;

    public string ParseCall =>
        $"""
         _{TargetName} ??= new List<{ClassName}>();
         var _{TargetName}Element = await {ClassName}.ParseAsync(reader);
         _{TargetName}.Add(_{TargetName}Element);
         """;
}