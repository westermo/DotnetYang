using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Key : Statement
{
    public Key(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "key";

    public override string ToCode()
    {
        Parent?.Attributes.Add($"Key(nameof({MakeName(Argument.Replace("\n", "").Replace("\"", "'"))}))");
        return string.Empty;
    }
}