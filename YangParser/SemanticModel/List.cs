using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class List : Statement, IClassSource
{
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(StateData.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Key.Keyword),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(MinElements.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(OrderedBy.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Unique.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword)
    ];

    public List(YangStatement statement)
    {
        if (statement.Keyword != Keyword)
            throw new InvalidOperationException($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}");
        Argument = statement.Argument!.ToString();
        ValidateChildren(statement);
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        var key = Children.OfType<Key>().FirstOrDefault();
        var stateData = Children.OfType<StateData>().FirstOrDefault();
        if (stateData == null || stateData.Argument != "true")
        {
            if (key == null) throw new InvalidOperationException("List must have a key when it represents configuration data");
        }
    }

    public const string Keyword = "list";
}
