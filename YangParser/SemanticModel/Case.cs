using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

/// <summary>
/// 7.9.2.  The choice's case Statement
///
///   The "case" statement is used to define branches of the choice.  It
///   takes as an argument an identifier, followed by a block of
///   substatements that holds detailed case information.
///
///   The identifier is used to identify the case node in the schema tree.
///   A case node does not exist in the data tree.
///
///   Within a "case" statement, the "anyxml", "choice", "container",
///   "leaf", "list", "leaf-list", and "uses" statements can be used to
///   define child nodes to the case node.  The identifiers of all these
///   child nodes MUST be unique within all cases in a choice.  For
///   example, the following is illegal:
///
///     choice interface-type {     // This example is illegal YANG
///         case a {
///             leaf ethernet { ... }
///         }
///         case b {
///             container ethernet { ...}
///         }
///     }
///
///
///   As a shorthand, the "case" statement can be omitted if the branch
///   contains a single "anyxml", "container", "leaf", "list", or
///   "leaf-list" statement.  In this case, the identifier of the case node
///   is the same as the identifier in the branch statement.  The following
///   example:
///
///     choice interface-type {
///         container ethernet { ... }
///     }
///
///   is equivalent to:
///
///     choice interface-type {
///         case ethernet {
///             container ethernet { ... }
///         }
///     }
///
///   The case identifier MUST be unique within a choice.
///
///7.9.2.1.  The case's Substatements
///
///                 +--------------+---------+-------------+
///                 | substatement | section | cardinality |
///                 +--------------+---------+-------------+
///                 | anyxml       | 7.10    | 0..n        |
///                 | choice       | 7.9     | 0..n        |
///                 | container    | 7.5     | 0..n        |
///                 | description  | 7.19.3  | 0..1        |
///                 | if-feature   | 7.18.2  | 0..n        |
///                 | leaf         | 7.6     | 0..n        |
///                 | leaf-list    | 7.7     | 0..n        |
///                 | list         | 7.8     | 0..n        |
///                 | reference    | 7.19.4  | 0..1        |
///                 | status       | 7.19.2  | 0..1        |
///                 | uses         | 7.12    | 0..n        |
///                 | when         | 7.19.5  | 0..1        |
///                 +--------------+---------+-------------+
/// </summary>
public class Case : Statement, IClassSource, IXMLSource
{
    public List<string> Comments { get; } = new();

    public Case(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "case";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        var nodes = Children.Select(c => c.ToCode()).ToArray();
        return $$"""
                 public {{MakeName(Parent!.Argument)}}Choice({{TargetName}}Case input)
                 {
                     {{TargetName}} = input;
                 }
                 public {{TargetName}}Case? {{TargetName}};
                 {{DescriptionString}}{{AttributeString}}
                 public class {{TargetName}}Case
                 {
                    {{Indent(string.Join("\n", nodes))}}
                    {{Indent(XmlFunctionWithInvisibleSelf())}}
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);
}