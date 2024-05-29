using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Submodule : Statement, ITopLevelStatement
{
    public Dictionary<string, string> ImportedModules { get; } = [];
    public Submodule(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Usings = new();
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
        }
    }

    public List<TypeDefinition> HiddenDefinitions { get; } = [];
    private bool IsExpanded = false;
    public void Expand()
    {
        if (IsExpanded) return;
        foreach (var child in Children)
        {
            ExpandPrefixes(child);
        }
        IsExpanded = true;
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Augment.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(BelongsTo.Keyword, Cardinality.Required),
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
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Organization.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Revision.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Rpc.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(SemanticModel.Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(YangVersion.Keyword, Cardinality.Required),
    ];

    public const string Keyword = "submodule";

    public override string ToCode()
    {
        return string.Join("\n", Children.Select(child => child.ToCode()).ToArray());
    }

    public Dictionary<string, string> Usings { get; }

    private void ExpandPrefixes(IStatement statement)
    {

        if (!statement.Argument.Contains(" "))
        {
            var argPrefix = statement.Argument.Split(':');
            if (argPrefix.Length > 1 && argPrefix.Length < 3) //ignore cases where there are multiple colons, since that's an XML-namespace reference
            {
                if (Usings.ContainsKey(argPrefix[0]))
                {
                    statement.Argument = statement.Argument.Replace(argPrefix[0] + ":", Usings[argPrefix[0]]);
                }
                else
                {
                    Log.Write($"No prefix found for {argPrefix[0]} in {Argument}");
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

}

public class BelongsTo : Statement
{
    public BelongsTo(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "belongs-to";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Prefix.Keyword, Cardinality.Required)
    ];
}