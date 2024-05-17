using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class RequireInstance : Statement
{
    public RequireInstance(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Value = bool.Parse(Argument);
    }

    public const string Keyword = "require-instance";

    public bool Value { get; }
}
