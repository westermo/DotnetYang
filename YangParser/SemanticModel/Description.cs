namespace YangParser.SemanticModel;

public class Description : Statement
{
    public Description(YangStatement statement)
    {
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
    }

    public const string Keyword = "description";
}