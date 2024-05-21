using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Refine : Statement
{
    public Refine(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "refine";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Presence.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MinElements.Keyword),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(Config.Keyword)
    ];
}