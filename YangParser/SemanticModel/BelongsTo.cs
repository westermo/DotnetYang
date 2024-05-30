using YangParser.Parser;

namespace YangParser.SemanticModel;

public class BelongsTo : Statement
{
    public BelongsTo(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "belongs-to";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(SemanticModel.Prefix.Keyword, Cardinality.Required)
    ];

    public override string ToCode()
    {
        return string.Empty;
    }
}