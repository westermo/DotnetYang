using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Feature : Statement
{
    public Feature(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Status.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public const string Keyword = "feature";

    public override string ToCode()
    {
        Parent?.Attributes.Add($"ProvidesFeature(\"{Argument}\")");
        return $"//Feature Declaration: {Argument}, Parent is {Parent?.GetType()}";
    }
}