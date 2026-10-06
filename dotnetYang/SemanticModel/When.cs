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
    /// When a <c>when</c> is distributed from an <c>augment</c> to the augment's
    /// data children, the XPath was authored with the augment <b>target</b> as
    /// the context node — not the leaf/container it ends up attached to. This
    /// property records that original target so the translator evaluates the XPath
    /// in the correct schema context. The validation emitter evaluates it at the
    /// closest data-node ancestor of this statement (skipping choice/case/uses),
    /// per RFC 7950 §7.21.5.
    /// <para>
    /// If <c>null</c>, the context is the node the <c>when</c> is attached to
    /// (normal case for <c>when</c> authored directly on a data node), or its
    /// closest data-node ancestor when attached to a choice or case.
    /// </para>
    /// </summary>
    public IStatement? OriginalContext { get; set; }

    /// <summary>
    /// Set when this <c>when</c> was copied from a <c>uses</c> onto a node contributed
    /// by the grouping. Its context is then the closest data-node ancestor of the node
    /// it ends up attached to (i.e. the parent of the <c>uses</c>), resolved after all
    /// grouping/augment expansion has placed the node in its final position.
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