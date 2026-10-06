using YangParser.Parser;

namespace YangParser.SemanticModel;

public class AnyXml(YangStatement statement) : AnyNodeStatement(statement, Keyword)
{
    public const string Keyword = "anyxml";
}
