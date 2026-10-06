using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class When : Statement, IUnexpandable
{
    public When(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public const string Keyword = "when";

    /// <summary>
    /// The original XPath expression text (normalised to one line).
    /// Used by the validation emitter only as a documentation comment;
    /// the runtime check is compiled C# generated from the parsed AST.
    /// </summary>
    public string XPathExpression => SingleLine(Argument).Replace("\"", "\\\"");

    /// <summary>
    /// Set when this <c>when</c> was distributed from an <c>augment</c> or <c>uses</c>
    /// onto the nodes they contribute. Per RFC 7950 §7.21.5 the XPath context is then
    /// the augment target / the parent of the <c>uses</c> (or the closest data-node
    /// ancestor if that is a choice or case) — i.e. the closest data-node ancestor of
    /// the node it ends up attached to, resolved after all grouping/augment expansion
    /// has placed the node in its final position.
    /// <para>
    /// If <c>false</c>, the context is the node the <c>when</c> is attached to
    /// (normal case for <c>when</c> authored directly on a data node), or its
    /// closest data-node ancestor when attached to a choice or case.
    /// </para>
    /// </summary>
    public bool ContextIsParent { get; set; }

    public override string ToCode()
    {
        while (Argument.Contains("  "))
        {
            Argument = Argument.Replace("  ", " ");
        }

        return string.Empty;
    }
}