using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Revision : Statement
{
    public Revision(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        Value = DateTime.Parse(Argument);
    }
    public const string Keyword = "revision";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword)
    ];
    public DateTime Value { get; }
}
