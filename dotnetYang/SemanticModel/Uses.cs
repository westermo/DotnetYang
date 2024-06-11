using System;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Uses : Statement, IUnexpandable
{
    public Uses(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "uses";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Augment.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Refine.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(When.Keyword)
    ];
}