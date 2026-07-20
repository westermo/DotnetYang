// XPath 1.0 AST node definitions used by the YANG -> C# source generator
// to translate `when` and `must` expressions into compiled tree walks.
//
// Only a useful subset of the W3C XPath 1.0 grammar is modelled here;
// anything we don't currently translate is still represented as an AST
// node so the translator can fall back gracefully.

using System.Collections.Generic;

namespace YangParser.SemanticModel.XPath;

internal abstract class XPathExpr
{
    /// <summary>
    /// The original source text of this expression node, kept for diagnostics
    /// and so we can place it in a comment alongside the translated code.
    /// </summary>
    public string SourceText { get; set; } = string.Empty;
}

internal sealed class StringLiteralExpr : XPathExpr
{
    public StringLiteralExpr(string value) { Value = value; }
    public string Value { get; }
}

internal sealed class NumberLiteralExpr : XPathExpr
{
    public NumberLiteralExpr(double value) { Value = value; }
    public double Value { get; }
}

internal sealed class VariableRefExpr : XPathExpr
{
    public VariableRefExpr(string? prefix, string name) { Prefix = prefix; Name = name; }
    public string? Prefix { get; }
    public string Name { get; }
}

internal sealed class FunctionCallExpr : XPathExpr
{
    public FunctionCallExpr(string? prefix, string name, IReadOnlyList<XPathExpr> args)
    {
        Prefix = prefix;
        Name = name;
        Arguments = args;
    }
    public string? Prefix { get; }
    public string Name { get; }
    public IReadOnlyList<XPathExpr> Arguments { get; }
}

internal enum BinaryOp
{
    Or, And,
    Eq, Ne,
    Lt, Le, Gt, Ge,
    Add, Sub,
    Mul, Div, Mod,
    Union
}

internal sealed class BinaryExpr : XPathExpr
{
    public BinaryExpr(BinaryOp op, XPathExpr left, XPathExpr right)
    {
        Op = op;
        Left = left;
        Right = right;
    }
    public BinaryOp Op { get; }
    public XPathExpr Left { get; }
    public XPathExpr Right { get; }
}

internal sealed class UnaryMinusExpr : XPathExpr
{
    public UnaryMinusExpr(XPathExpr operand) { Operand = operand; }
    public XPathExpr Operand { get; }
}

internal enum XPathAxis
{
    Child,
    Parent,
    Self,
    Descendant,
    DescendantOrSelf,
    Ancestor,
    AncestorOrSelf,
    Following,
    FollowingSibling,
    Preceding,
    PrecedingSibling,
    Attribute,
    Namespace
}

internal abstract class NodeTest
{
}

internal sealed class NameTest : NodeTest
{
    public NameTest(string? prefix, string localName) { Prefix = prefix; LocalName = localName; }
    public string? Prefix { get; }
    /// <summary>Local name, or "*" for wildcard.</summary>
    public string LocalName { get; }
}

internal sealed class NodeTypeTest : NodeTest
{
    public NodeTypeTest(string typeName, string? literalArg = null)
    {
        TypeName = typeName;
        LiteralArg = literalArg;
    }
    /// <summary>One of "comment", "text", "processing-instruction", "node".</summary>
    public string TypeName { get; }
    public string? LiteralArg { get; }
}

internal sealed class XPathStep
{
    public XPathStep(XPathAxis axis, NodeTest test, IReadOnlyList<XPathExpr> predicates)
    {
        Axis = axis;
        Test = test;
        Predicates = predicates;
    }
    public XPathAxis Axis { get; }
    public NodeTest Test { get; }
    public IReadOnlyList<XPathExpr> Predicates { get; }
}

internal sealed class PathExpr : XPathExpr
{
    /// <summary>
    /// If true, the path begins at the data tree root (i.e. starts with '/').
    /// </summary>
    public bool IsAbsolute { get; }

    /// <summary>
    /// Optional filter expression that the path applies steps onto (e.g.
    /// <c>function()/foo</c>). Mutually exclusive with absolute-from-root.
    /// </summary>
    public XPathExpr? Filter { get; }

    public IReadOnlyList<XPathStep> Steps { get; }

    public PathExpr(bool isAbsolute, XPathExpr? filter, IReadOnlyList<XPathStep> steps)
    {
        IsAbsolute = isAbsolute;
        Filter = filter;
        Steps = steps;
    }
}

internal sealed class FilterExpr : XPathExpr
{
    public FilterExpr(XPathExpr primary, IReadOnlyList<XPathExpr> predicates)
    {
        Primary = primary;
        Predicates = predicates;
    }
    public XPathExpr Primary { get; }
    public IReadOnlyList<XPathExpr> Predicates { get; }
}

internal sealed class ParenExpr : XPathExpr
{
    public ParenExpr(XPathExpr inner) { Inner = inner; }
    public XPathExpr Inner { get; }
}
