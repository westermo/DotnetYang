using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class CompilationUnit : Statement
{
    public CompilationUnit(Module[] modules) : base(new YangStatement(String.Empty, string.Empty, [], new Metadata(string.Empty, new Parser.Position(), 0)))
    {
        Children = modules;
    }
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Module.Keyword, Cardinality.ZeroOrMore),
    ];
}

public class Module : Statement
{
    public Module(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        XmlNamespace = Children.First(child => child is Namespace);
    }

    public IStatement XmlNamespace { get; set; }

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

    public string Filename => MakeNamespace(Argument) + "." + "Module.cs";
    public const string Keyword = "module";

    public override string ToCode()
    {
        var usings = new Dictionary<string, string>();
        string ns = MakeNamespace(Argument);
        foreach (var child in Children)
        {
            if (child is Import import)
            {
                var use = MakeNamespace(import.Argument) + ".Module.";
                if (child.Children.FirstOrDefault(x => x is Prefix) is Prefix prefix)
                {
                    usings[prefix.Argument] = use;
                }
                else
                {
                    usings[use] = use;
                }
            }

            if (child is Prefix modulePrefix)
            {
                usings[modulePrefix.Argument] = string.Empty;
            }
        }

        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var raw = $$"""
                    using System;
                    using System.Collections.Generic;
                    using System.Runtime.CompilerServices;
                    using System.Xml.Linq;
                    using Yang.Attributes;
                    {{string.Join("\n", usings.Values.Where(p => p != string.Empty).Select(value => $"using {value.Replace(".Module.", "")};"))}}
                    namespace {{ns}};
                    {{DescriptionString}}
                    {{AttributeString}}
                    public static class Module
                    {
                        {{string.Join("\n\t", nodes.Select(Indent))}}
                    }
                    """;
        foreach (var prefix in usings.Keys)
        {
            raw = raw.Replace(" " + prefix + ":", " " + usings[prefix]);
        }

        return raw;
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