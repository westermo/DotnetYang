using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Module : Statement, ITopLevelStatement
{
    public Module(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        XmlNamespace = Children.First(child => child is Namespace);
        Usings = new();
        foreach (var import in Children.OfType<Import>())
        {
            var use = MakeNamespace(import.Argument) + ".YangNode.";
            var prefix = import.GetChild<Prefix>().Argument;
            Usings[prefix] = use;
        }

        Usings[this.GetChild<Prefix>().Argument] = MakeNamespace(Argument) + ".YangNode.";
    }

    public IStatement XmlNamespace { get; set; }
    public Dictionary<string, string> Usings { get; }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Augment.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Contact.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Deviation.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Extension.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Feature.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Identity.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Import.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Include.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Namespace.Keyword, Cardinality.Required),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Organization.Keyword),
        new ChildRule(Prefix.Keyword, Cardinality.Required),
        new ChildRule(Reference.Keyword),
        new ChildRule(Revision.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Rpc.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(YangVersion.Keyword),
    ];

    public string Filename => "YangModules/" + Argument.Split('-').First() + "/" + Argument + ".cs";
    public const string Keyword = "module";

    public override string ToCode()
    {
        string ns = MakeNamespace(Argument);

        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var raw = $$"""
                    using System;
                    using System.Collections.Generic;
                    using System.Runtime.CompilerServices;
                    using System.Xml.Linq;
                    using System.Text.RegularExpressions;
                    using Yang.Attributes;
                    {{string.Join("\n", Usings.Values.Where(p => p != MakeNamespace(Argument) + ".YangNode.").Select(value => $"using {value.Replace(".YangNode.", "")};").Distinct())}}
                    namespace {{ns}};
                    {{DescriptionString}}{{AttributeString}}
                    public class YangNode
                    {
                        {{string.Join("\n\t", Usings.Select(p => $"//Importing {p.Value} as {p.Key}"))}}
                        {{string.Join("\n\t", nodes.Select(Indent))}}
                    }
                    """;
        raw = ReplacePrefixes(raw);

        return raw;
    }

    private string ReplacePrefixes(string raw)
    {
        foreach (var prefix in Usings.Keys)
        {
            raw = raw.Replace(" " + prefix + ":", " " + Usings[prefix]);
            raw = raw.Replace("(" + prefix + ":", "(" + Usings[prefix]);
        }

        return raw;
    }

    public void ExpandPrefixes(IStatement statement)
    {
        foreach (var prefix in Usings.Keys)
        {
            statement.Argument = statement.Argument.Replace(prefix + ":", Usings[prefix]);
            if (statement is KeywordReference keywordReference)
            {
                if (keywordReference.ReferenceNamespace == prefix)
                {
                    keywordReference.ReferenceNamespace = Usings[prefix];
                }
            }
        }

        foreach (var child in statement.Children)
        {
            ExpandPrefixes(child);
        }
    }

    // public string Capability
    // {
    //     get
    //     {
    //         StringBuilder builder = new StringBuilder(NameSpace);
    //         builder.Append("?").Append($"module={Argument}");
    //         if (Revision.HasValue)
    //         {
    //             builder.Append($"&amp;revision={Revision.Value:yyyy-mm-dd}");
    //         }
    //
    //         return builder.ToString();
    //     }
    // }
}

public interface ITopLevelStatement : IStatement
{
    public Dictionary<string, string> Usings { get; }
    public void ExpandPrefixes(IStatement statement);
}