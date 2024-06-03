using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Module : Statement, ITopLevelStatement, IXMLParseable
{
    public Module(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        var localPrefix = this.GetChild<Prefix>().Argument;
        var localNS = MakeNamespace(Argument) + ".YangNode.";
        XmlNamespace = (Children.First(child => child is Namespace).Argument, string.Empty);
        Usings = new()
        {
            [localPrefix] = localNS
        };
        ImportedModules[localPrefix] = Argument;
        MyNamespace = localNS;
        PrefixToNamespaceTable[localPrefix] = XmlNamespace.Value.Namespace;

        foreach (var child in this.Unwrap())
        {
            if (child is Uses use)
            {
                Uses.Add(use);
            }

            if (child is Grouping grouping)
            {
                Groupings.Add(grouping);
            }

            if (child is Augment augment)
            {
                Augments.Add(augment);
            }

            if (child is Import import)
            {
                Imports.Add(import);
                var reference = MakeNamespace(import.Argument) + ".YangNode.";
                var prefix = import.GetChild<Prefix>().Argument;
                Usings[prefix] = reference;
                ImportedModules[prefix] = import.Argument;
            }

            if (child is TypeDefinition typeDefinition)
            {
                if (typeDefinition.IsUnderGrouping())
                {
                    HiddenDefinitions.Add(typeDefinition);
                    typeDefinition.Parent?.Replace(typeDefinition, []);
                }
            }

            if (child is Extension extension)
            {
                Extensions.Add(extension);
            }

            if (child is Feature feature)
            {
                Features.Add(feature);
            }

            if (child is Revision revision)
            {
                Revisions.Add(revision);
            }
        }
    }

    private List<Feature> Features { get; } = [];
    private List<Revision> Revisions { get; } = [];
    public Dictionary<string, string> PrefixToNamespaceTable { get; } = [];
    public Dictionary<string, string> ImportedModules { get; } = [];
    public Dictionary<string, string> Usings { get; }
    public string MyNamespace { get; private set; }

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
        new ChildRule(SemanticModel.Namespace.Keyword, Cardinality.Required),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Organization.Keyword),
        new ChildRule(SemanticModel.Prefix.Keyword, Cardinality.Required),
        new ChildRule(Reference.Keyword),
        new ChildRule(Revision.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Rpc.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(SemanticModel.Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(YangVersion.Keyword),
    ];

    public string Filename => "YangModules/" + Argument.Split('-').First() + "/" + Argument + ".cs";
    public const string Keyword = "module";

    public override string ToCode()
    {
        string ns = MakeNamespace(Argument);

        var nodes = Children.Select(child => child.ToCode()).Select(Indent).ToArray();
        var extraDefinitions = HiddenDefinitions.Select(t => Indent(t.ToCode())).ToArray();
        var raw = $$"""
                    using System;
                    using System.Xml;
                    using System.Text;
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
                        public const string ModuleName = "{{Argument}}";
                        public const string Revision = "{{Revisions.FirstOrDefault()?.Argument}}";
                        public static string[] Features = [{{string.Join(",", Features.Select(f => $"\"{f.Argument}\""))}}];
                        {{string.Join("\n\t", nodes)}}
                        {{string.Join("\n\t", extraDefinitions)}}
                        {{Indent(ReadFunction())}}
                        {{Indent(WriteFunction())}}
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
            raw = raw.Replace("\t" + prefix + ":", "\t" + Usings[prefix]);
            raw = raw.Replace("(" + prefix + ":", "(" + Usings[prefix]);
        }

        return raw;
    }

    private bool IsExpanded;

    public void Expand()
    {
        if (IsExpanded) return;
        foreach (var child in Children)
        {
            ExpandPrefixes(child);
        }

        IsExpanded = true;
    }

    private void ExpandPrefixes(IStatement statement)
    {
        if (statement is not IUnexpandable)
        {
            if (!statement.Argument.Contains(' ') && !statement.Argument.Contains('(') &&
                !statement.Argument.Contains('['))
                //Only occurs in string arguments, which are unaffected by prefixes, 
                // and in function calls, which are unaffected by prefixes
                // or in regex expressions, which are unaffected by prefixes
            {
                var argPrefix = statement.Argument.Split(':');
                if (argPrefix.Length > 1 &&
                    argPrefix.Length <
                    3) //ignore cases where there are multiple colons, since that's an XML-namespace reference
                {
                    if (Usings.ContainsKey(argPrefix[0]))
                    {
                        statement.Argument = statement.Argument.Replace(argPrefix[0] + ":", Usings[argPrefix[0]]);
                    }
                    else
                    {
                        Log.Write($"No prefix found for {statement.Argument} in module {Argument}");
                    }
                }
            }
        }

        foreach (var child in statement.Children)
        {
            ExpandPrefixes(child);
        }
    }

    public List<Uses> Uses { get; } = [];
    public List<Grouping> Groupings { get; } = [];
    public List<Augment> Augments { get; } = [];
    public List<Import> Imports { get; } = [];
    public List<Extension> Extensions { get; } = [];
    public List<TypeDefinition> HiddenDefinitions { get; } = [];

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
    public string? TargetName => MakeName(Argument);
    public string ClassName => MakeNamespace(Argument) + ".YangNode";
}

public interface ITopLevelStatement : IStatement
{
    public Dictionary<string, string> Usings { get; }
    public void Expand();
    public List<Uses> Uses { get; }
    public List<Grouping> Groupings { get; }
    public List<Augment> Augments { get; }
    public List<Import> Imports { get; }
    public List<Extension> Extensions { get; }
    Dictionary<string, string> ImportedModules { get; }
}