using YangParser.Parser;

namespace YangParser.SemanticModel;

public class AnyData(YangStatement statement) : AnyNodeStatement(statement, Keyword)
{
    public const string Keyword = "anydata";
}
