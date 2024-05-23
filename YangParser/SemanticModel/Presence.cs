using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Presence : Statement
{
    public Presence(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "presence";

    public override string ToCode()
    {
        Parent?.Attributes.Add($"Presence(\"{SingleLine(Argument)}\")");
        return string.Empty;
    }
}