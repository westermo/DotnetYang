using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Type : Statement
{
    public Type(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "type";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Enum.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Bit.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Length.Keyword),
        new ChildRule(Pattern.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Path.Keyword),
        new ChildRule(Range.Keyword),
        new ChildRule(RequireInstance.Keyword),
        new ChildRule(FractionDigits.Keyword),
        new ChildRule(Base.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
    ];
}