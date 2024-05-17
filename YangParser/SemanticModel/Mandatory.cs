using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Mandatory : Statement
{
    public Mandatory(YangStatement statement)
    {
        Value = bool.Parse(statement.Argument!.ToString());
        ValidateChildren(statement);
    }

    public const string Keyword = "mandatory";

    public bool Value { get; }
}