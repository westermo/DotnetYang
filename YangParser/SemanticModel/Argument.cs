using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Argument : Statement
{
    public Argument(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "argument";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(YinElement.Keyword),
    ];
}
