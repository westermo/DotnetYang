using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class YinElement : Statement
{
    public YinElement(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        Value = bool.Parse(Argument);
    }

    public const string Keyword = "yin-element";

    public bool Value { get; }
}