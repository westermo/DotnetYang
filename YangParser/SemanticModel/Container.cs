using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Container : Statement, IClassSource, IXMLParseable
{
    public List<string> Comments { get; } = new();

    public Container(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public Container(string argument, Metadata metadata) : base(
        new YangStatement(
            string.Empty,
            Keyword,
            [],
            metadata,
            argument))
    {
        IsPlaceholder = true;
    }

    public bool IsPlaceholder { get; set; }

    public const string Keyword = "container";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Action.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Config.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Presence.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword)
    ];

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        string property = $"public{KeywordString}{ClassName}? {TargetName} {{ get; set; }}";
        return $$"""
                 {{property}}
                 {{DescriptionString}}{{AttributeString}}
                 public class {{ClassName}}
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    public string TargetName
    {
        get
        {
            var name = Argument;
            if (Parent!.Argument == Argument)
            {
                name = "sub-" + name;
            }

            return MakeName(name);
        }
    }

    public string ClassName => TargetName + "Container";
}