using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class YangVersion : Statement
{
    public YangVersion(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        Value = new Version(Argument);
    }
    public const string Keyword = "yang-version";
    public Version Value {get;}
}
