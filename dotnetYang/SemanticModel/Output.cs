using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Output(YangStatement statement) : OperationDataStatement(statement, Keyword)
{
    public const string Keyword = "output";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore)
    ];

    protected override string XmlElementName => "rpc-reply";
}
