using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Bit : Statement
{
    public Bit(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "bit";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Position.Keyword)
    ];

    public override string ToCode()
    {
        // foreach (var child in Children)
        // {
        //     child.ToCode();
        // }
        Parent?.Attributes.Add("Flags");
        var assignment = Children.FirstOrDefault(child => child is Position)?.Argument;
        int index;
        if (string.IsNullOrWhiteSpace(assignment))
        {
            index = Array.IndexOf(Parent!.Children, this);
        }
        else
        {
            if (!int.TryParse(assignment, out index))
            {
                throw new SemanticError($"Could not parse bit position from value '{assignment}'", Source);
            }
        }

        return $"""
                {DescriptionString}{AttributeString}
                {MakeName(Argument)} = {Math.Pow(2, index)},
                """;
    }
}