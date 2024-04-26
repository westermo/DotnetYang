using System;
using System.Linq;

namespace YangParser.SemanticModel;

public class Unique : Statement
{
    public Unique(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Identifiers = Argument.Split(' ');
    }

    public const string Keyword = "unique";

    public string[] Identifiers { get; }
}