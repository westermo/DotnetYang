using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class List : Statement, IClassSource, IXMLValue
{
    private YangStatement m_source;

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

        m_source = statement;
    }

    public const string Keyword = "list";
    public List<string> Comments { get; } = new();


    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        string property =
            $"\n{DescriptionString}\npublic{KeywordString}List<{MakeName(Argument)}Entry> {TargetName} {{ get; }} = new();";
        return $$"""
                 {{property}}
                 {{AttributeString}}
                 public class {{MakeName(Argument)}}Entry
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(XmlFunction())}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);

    public string WriteCall
    {
        get
        {
            var pre = string.IsNullOrWhiteSpace(Prefix) ? "null" : $"\"{Prefix}\"";
            return $$"""
                     if({{TargetName}} != null)
                     {
                         foreach(var element in {{TargetName}})
                         {
                             await element!.WriteXML(writer);
                         }
                     }
                     """;
        }
    }
}