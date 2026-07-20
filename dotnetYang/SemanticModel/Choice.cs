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
        var isMandatory = this.TryGetChild<Mandatory>(out var mandatory) && mandatory!.Value;
        var nullable = isMandatory && !Children.Any(c => c is When) ? string.Empty : "?";
        var parentName = ParentClassName;
        string property;
        if (parentName is null)
        {
            property =
                $"public{KeywordString}{MakeName(Argument)}Choice{nullable} {MakeName(Argument)} {{ get; set; }}";
        }
        else
        {
            property = $$"""
                         private {{MakeName(Argument)}}Choice{{nullable}} _{{MakeName(Argument)}};
                         public{{KeywordString}}{{MakeName(Argument)}}Choice{{nullable}} {{MakeName(Argument)}}
                         {
                             get => _{{MakeName(Argument)}};
                             set
                             {
                                 if (_{{MakeName(Argument)}} is not null) _{{MakeName(Argument)}}.YangParent = null;
                                 _{{MakeName(Argument)}} = value;
                                 if (value is not null) value.YangParent = this;
                             }
                         }
                         """;
        }
        var parentDecl = ParentPropertyDeclaration();
        var validate = global::YangParser.SemanticModel.XPath.ValidateEmitter.EmitValidateMethod(this);

        return $$"""
                 {{property}}
                 {{DescriptionString}}{{AttributeString}}
                 public class {{TargetName}}Choice
                 {
                     {{Indent(parentDecl)}}
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(WriteFunctionInvisibleSelf())}}
                     {{Indent(ReadFunctionWithInvisibleSelf())}}
                     {{Indent(validate)}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);
    public string ClassName => TargetName + "Choice";

    private IEnumerable<IStatement> directChildren =>
        Children.Where(c => c is (IXMLParseable or IXMLReadValue) and not Case);

    public IEnumerable<string> SubTargets => directChildren
        .Select(c => c.XmlObjectName).Concat(Children.OfType<Case>().SelectMany(c => c.SubTargets)).Distinct();
}