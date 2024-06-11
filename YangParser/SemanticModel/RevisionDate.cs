using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class RevisionDate : Statement
{
    public RevisionDate(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        if (!DateTime.TryParse(Argument, out _)) throw new InvalidOperationException($"Invalid date format '{Argument}'");
    }

    public const string Keyword = "revision-date";
}
