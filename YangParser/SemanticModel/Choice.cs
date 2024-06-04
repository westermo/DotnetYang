using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Choice : Statement, IClassSource, IXMLParseable
{
    public Choice(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "choice";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Case.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Config.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(When.Keyword, Cardinality.ZeroOrMore)
    ];

    public List<string> Comments { get; } = new();

    public override string ToCode()
    {
        var nodes = Children.Where(t => t is not DefaultValue).Select(child => child.ToCode()).ToArray();
        string property = $"public{KeywordString}{MakeName(Argument)}Choice? {MakeName(Argument)} {{ get; set; }}";

        return $$"""
                 {{property}}
                 {{DescriptionString}}{{AttributeString}}
                 public class {{TargetName}}Choice
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(WriteFunctionInvisibleSelf())}}
                     {{Indent(ReadFunctionWithInvisibleSelf())}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);
    public string ClassName => TargetName + "Choice";

    private IEnumerable<IStatement> directChildren =>
        Children.Where(c => c is (IXMLParseable or IXMLReadValue) and not Case);

    private IEnumerable<IStatement> caseChildren =>
        Children.OfType<Case>().SelectMany(c => c.Children).Where(c => c is IXMLParseable or IXMLReadValue);

    public IEnumerable<string> SubTargets => directChildren.Concat(caseChildren)
        .Select(c => c.XmlObjectName).Distinct();
}