using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Augment : Statement, IUnexpandable
{
    public Augment(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "augment";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Case.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Refine.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Deviation.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Rpc.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Extension.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Identity.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var name = MakeName(Argument.Split('/', ':').Last()) + "Augmentation";
        Attributes.Add($"Target(\"{Argument.Replace("\n", "")}\")");
        //TODO: Add Augmentation logic
        return $$"""
                 /*
                 {{this}}
                 */
                 """;
    }
    protected override void ValidateParent()
    {
        this.GetModule()?.Augments.Add(this);
    }
}