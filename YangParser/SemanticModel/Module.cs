using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Module : TopLevelStatement, IXMLParseable
{
    public Module(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        var localPrefix = this.GetChild<Prefix>().Argument;
        var localNS = MakeNamespace(Argument) + ".YangNode.";
        XmlNamespace = (Children.First(child => child is Namespace).Argument, string.Empty);
        Usings[localPrefix] = localNS;
        ImportedModules[localPrefix] = Argument;
        MyNamespace = localNS;
        PrefixToNamespaceTable[localPrefix] = XmlNamespace.Value.Namespace;
    }

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
        var interfaceDefinition = Parent is CompilationUnit unit && !string.IsNullOrWhiteSpace(unit.MyNamespace) &&
                                  Rpcs.Count + Actions.Count + Notifications.Count > 0
            ? $$"""
                namespace {{unit.MyNamespace}}
                {
                    public partial interface IYangServer
                    {
                        {{Indent(Indent(string.Join("\n", Rpcs.Select(rpc => rpc.ServerDeclaration))))}}
                        {{Indent(Indent(string.Join("\n", Actions.Select(action => action.ServerDeclaration))))}}
                        {{Indent(Indent(string.Join("\n", Notifications.Select(notification => notification.ServerDeclaration))))}}
                    }
                }
                """
            : string.Empty;
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
                    {{interfaceDefinition}}
                    namespace {{ns}}{
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