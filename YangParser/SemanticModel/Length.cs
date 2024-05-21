using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Length : Statement
{
    public Length(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "length";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Reference.Keyword),
    ];
}
