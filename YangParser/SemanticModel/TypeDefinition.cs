using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class TypeDefinition : Statement, IClassSource
{
    public TypeDefinition(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "typedef";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
    ];
}