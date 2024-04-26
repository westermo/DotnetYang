using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class YangVersion : Statement
{
    public YangVersion(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        Value = new Version(Argument);
    }
    public const string Keyword = "yang-version";
    public Version Value {get;}
}
