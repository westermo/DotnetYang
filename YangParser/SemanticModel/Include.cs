using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Include : Statement
{
    public Include(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }
    public const string Keyword = "include";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(RevisionDate.Keyword),
    ];
}
