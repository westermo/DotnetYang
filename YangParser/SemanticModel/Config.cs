using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Config : Statement
{
    public Config(YangStatement statement)
    {
        ValidateChildren(statement);
        Value = bool.Parse(statement.Argument!.ToString());
    }

    public const string Keyword = "config";

    public bool Value { get; }
}