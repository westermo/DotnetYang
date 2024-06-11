using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Mandatory : Statement
{
    public Mandatory(YangStatement statement) : base(statement)
    {
        Value = bool.Parse(statement.Argument!.ToString());
        ValidateChildren(statement);
    }

    public const string Keyword = "mandatory";

    public bool Value { get; }

    public override string ToCode()
    {
        Parent?.Keywords.Add("required");
        return string.Empty;
    }
}