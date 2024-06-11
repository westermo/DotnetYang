using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Deviation : Statement
{
    public Deviation(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }
    public const string Keyword = "deviation";
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Deviate.Keyword, Cardinality.OneOrMore),
        new ChildRule(Reference.Keyword),
    ];
}
