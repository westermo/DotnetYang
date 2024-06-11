using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Description : Statement
{
    public Description(YangStatement statement) : base(statement)
    {
        
        ValidateChildren(statement);
    }

    public const string Keyword = "description";

    public override string ToCode()
    {
        return string.Empty;
    }
}