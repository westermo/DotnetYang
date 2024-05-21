using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Unique : Statement
{
    public Unique(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        Identifiers = Argument.Split(' ');
    }

    public const string Keyword = "unique";

    public string[] Identifiers { get; }
}