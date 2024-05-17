using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Reference : Statement, ICommentSource
{
    public Reference(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public const string Keyword = "reference";
}
