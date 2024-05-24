using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Submodule : Statement, ITopLevelStatement
{
    public Submodule(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Usings = new();
        foreach (var import in Children.OfType<Import>())
        {
            var use = MakeNamespace(import.Argument) + ".YangNode.";
            var prefix = import.GetChild<Prefix>().Argument;
            Usings[prefix] = use;
        }
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
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(YangVersion.Keyword, Cardinality.Required),
    ];

    public const string Keyword = "submodule";

    public override string ToCode()
    {
        return string.Join("\n", Children.Select(child => child.ToCode()).ToArray());
    }

    public Dictionary<string, string> Usings { get; }

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