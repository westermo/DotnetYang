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
        var xpath = SingleLine(Argument).Replace("\"", "\\\"");
        var hasErrorAppTag = this.TryGetChild<ErrorAppTag>(out var appTag);
        var hasErrorMessage = this.TryGetChild<ErrorMessage>(out var errorMessage);
        var extra = string.Empty;
        if (hasErrorAppTag)
        {
            extra += $", ErrorTag=\"{SingleLine(appTag!.Argument).Replace("\"", "\\\"")}\"";
        }

        if (hasErrorMessage)
        {
            extra += $", ErrorMessage=\"{SingleLine(errorMessage!.Argument).Replace("\"", "\\\"")}\"";
        }

        Parent?.Attributes.Add($"Must(\"{xpath}\"{extra})");
        return string.Empty;
    }
}