using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Namespace : Statement
{
    public Namespace(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "namespace";

    public override string ToCode()
    {
        return $"public const string Namespace = \"{Argument.Replace("\n","")}\";";
    }
}