using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Enum : Statement
{
    public Enum(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "enum";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Value.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword)
    ];

    public override string ToCode()
    {
        // foreach (var child in Children)
        // {
        //     child.ToCode();
        // }
        var assignment = Children.FirstOrDefault(child => child is Value)?.Argument;
        assignment = assignment is null ? null : $" = {assignment}";
        return $"""
                {DescriptionString}{AttributeString}
                {MakeName(Argument)}{assignment},
                """;
    }
}