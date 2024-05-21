using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Reference : Statement, ICommentSource
{
    public Reference(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "reference";
    public override string ToCode()
    {
        Parent?.Attributes.Add($"Reference(\"{Argument.Replace("\n","")}\")");
        return string.Empty;
    }
}
