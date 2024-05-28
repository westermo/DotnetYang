using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Identity : Statement
{
    public Identity(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "identity";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Base.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }
        return $"""
                {DescriptionString}{AttributeString}
                public static string {MakeName(Argument)}Identity = "{Argument}";
                """;
    }
}