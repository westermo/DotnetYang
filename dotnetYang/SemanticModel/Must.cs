using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Must : Statement
{
    public Must(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "must";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword)
    ];

    public override string ToCode()
    {
        // The XPath itself is intentionally NOT persisted as a string in the
        // generated code. error-app-tag / error-message are used by the
        // validation emitter when building exception messages.
        return string.Empty;
    }
}