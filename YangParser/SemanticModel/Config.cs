using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Config : Statement
{
    public Config(YangStatement statement) : base(statement)
    {
        ValidateChildren(statement);
        Value = bool.Parse(statement.Argument!.ToString());
    }

    public const string Keyword = "config";

    public bool Value { get; }

    public override string ToCode()
    {
        if (!Value) Parent?.Attributes.Add("NotConfigurationData");
        return string.Empty;
    }
}