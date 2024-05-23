using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class TypeDefinition : Statement, IFunctionSource
{
    private readonly YangStatement m_source;

    public TypeDefinition(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        BaseType = Children.OfType<Type>().First();
        m_source = statement;
    }

    public Type BaseType { get; }

    public const string Keyword = "typedef";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
    ];

    public List<string> Comments { get; } = new();

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        return HandleType(BaseType, Argument);
    }
}