namespace YangParser.SemanticModel;

public readonly struct ChildRule(string keyword, Cardinality cardinality = Cardinality.ZeroOrOne)
{
    public readonly string Keyword = keyword;
    public readonly Cardinality Cardinality = cardinality;
}