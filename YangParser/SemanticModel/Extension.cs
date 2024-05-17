using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Extension : Statement
{
    public Extension(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }
    public const string Keyword = "extension";
    public override ChildRule[] PermittedChildren { get; } = [
        new ChildRule(YangParser.SemanticModel.Argument.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
    ];
}
