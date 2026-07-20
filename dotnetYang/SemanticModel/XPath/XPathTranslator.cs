// XPath -> C# expression translator.
//
// Walks an XPath AST against the YANG schema and emits a C# boolean expression
// that runs against an instance of the generated class corresponding to the
// schema context. Where translation is not yet supported we throw
// <see cref="UntranslatableXPathException"/>; the caller is expected to fall
// back to a generated stub that throws <c>NotSupportedException</c> with the
// original XPath in the message.
//
// Subset currently supported:
//   * Boolean / numeric / string literals
//   * Function calls: true(), false(), not(x), current(), count(path),
//     string(path)
//   * Comparisons: =, !=, <, <=, >, >=
//   * Boolean operators: and, or
//   * Parenthesized expressions
//   * Location paths whose steps are abbreviated child names or '..' (with
//     leaf, container, list, choice children). Predicates other than
//     bare-step existence and key-equality on a YangList are unsupported.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel.XPath;

internal sealed class UntranslatableXPathException : Exception
{
    public UntranslatableXPathException(string message) : base(message) { }
}

internal enum CSharpKind
{
    /// <summary>A C# expression that evaluates to <c>bool</c>.</summary>
    Bool,
    /// <summary>A C# expression that evaluates to <c>double</c>.</summary>
    Number,
    /// <summary>A C# expression that evaluates to <c>string?</c>.</summary>
    String,
    /// <summary>A C# expression that references a generated YANG node instance (may be null).</summary>
    Node,
    /// <summary>A C# expression representing a possibly-empty sequence of node instances.</summary>
    NodeSet,
    /// <summary>A C# expression representing a leaf value (any boxed type).</summary>
    LeafValue
}

/// <summary>
/// Carries both the emitted C# code and the static type/schema we infer for it.
/// </summary>
internal readonly struct Translated
{
    public Translated(string code, CSharpKind kind, IStatement? schema = null)
    {
        Code = code;
        Kind = kind;
        Schema = schema;
    }

    public string Code { get; }
    public CSharpKind Kind { get; }
    /// <summary>The schema node this translated expression resolves to (for paths).</summary>
    public IStatement? Schema { get; }
}

internal sealed class XPathTranslator
{
    private readonly IStatement _origin;
    private readonly string _selfExpression;
    private readonly string _enclosingThis;

    public XPathTranslator(IStatement origin) : this(origin, "this", "this") { }

    /// <summary>
    /// Create a translator for an XPath whose authoring context is
    /// <paramref name="origin"/>.
    /// </summary>
    /// <param name="origin">The YANG schema node hosting the must/when.</param>
    /// <param name="selfExpression">
    /// The C# expression that represents the XPath context node ('.' /
    /// <c>current()</c>) within the generated Validate body. For a node-style
    /// origin (Container, List entry, Choice, Case) this is <c>"this"</c>; for
    /// a leaf-style origin this is the leaf accessor on the enclosing
    /// container instance, e.g. <c>"this.PortValue"</c>.
    /// </param>
    /// <param name="enclosingThis">
    /// The C# expression representing the enclosing generated class instance
    /// (i.e. <c>this</c> inside the Validate body). For node origins this is
    /// the same as <paramref name="selfExpression"/>; for leaf origins this is
    /// <c>"this"</c> so that '..' resolves to the container without going via
    /// the leaf value (leaves do not expose YangParent).
    /// </param>
    public XPathTranslator(IStatement origin, string selfExpression, string enclosingThis)
    {
        _origin = origin;
        _selfExpression = selfExpression;
        _enclosingThis = enclosingThis;
    }

    /// <summary>
    /// Translate the expression, requiring a boolean result. The returned
    /// string is a C# expression evaluating to <c>bool</c>.
    /// </summary>
    public string TranslateBoolean(XPathExpr expr)
    {
        var t = Translate(expr, _origin);
        return CoerceToBool(t);
    }

    private Translated Translate(XPathExpr expr, IStatement context)
    {
        switch (expr)
        {
            case ParenExpr p:
                var inner = Translate(p.Inner, context);
                return new Translated($"({inner.Code})", inner.Kind, inner.Schema);
            case StringLiteralExpr s:
                return new Translated($"\"{Escape(s.Value)}\"", CSharpKind.String);
            case NumberLiteralExpr n:
                return new Translated(n.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d",
                    CSharpKind.Number);
            case UnaryMinusExpr u:
                var operand = Translate(u.Operand, context);
                return new Translated($"(-({CoerceToNumber(operand)}))", CSharpKind.Number);
            case BinaryExpr b:
                return TranslateBinary(b, context);
            case FunctionCallExpr fc:
                return TranslateFunction(fc, context);
            case PathExpr pe:
                return TranslatePath(pe, context);
            case VariableRefExpr v:
                throw new UntranslatableXPathException(
                    $"XPath variable reference $${v.Name} is not supported in compile-time translation.");
            case FilterExpr f:
                // FilterExpr is PrimaryExpr Predicate*. We only handle the case
                // of zero predicates here (PrimaryExpr alone) via fall-through.
                if (f.Predicates.Count == 0) return Translate(f.Primary, context);
                throw new UntranslatableXPathException(
                    "XPath FilterExpr with predicates is not yet supported.");
        }
        throw new UntranslatableXPathException($"Unsupported XPath node type: {expr.GetType().Name}");
    }

    private Translated TranslateBinary(BinaryExpr b, IStatement context)
    {
        switch (b.Op)
        {
            case BinaryOp.And:
                {
                    var l = Translate(b.Left, context);
                    var r = Translate(b.Right, context);
                    return new Translated($"({CoerceToBool(l)} && {CoerceToBool(r)})", CSharpKind.Bool);
                }
            case BinaryOp.Or:
                {
                    var l = Translate(b.Left, context);
                    var r = Translate(b.Right, context);
                    return new Translated($"({CoerceToBool(l)} || {CoerceToBool(r)})", CSharpKind.Bool);
                }
            case BinaryOp.Eq:
            case BinaryOp.Ne:
            case BinaryOp.Lt:
            case BinaryOp.Le:
            case BinaryOp.Gt:
            case BinaryOp.Ge:
                return TranslateComparison(b, context);
            case BinaryOp.Add:
            case BinaryOp.Sub:
            case BinaryOp.Mul:
            case BinaryOp.Div:
            case BinaryOp.Mod:
                {
                    var l = Translate(b.Left, context);
                    var r = Translate(b.Right, context);
                    var ln = CoerceToNumber(l);
                    var rn = CoerceToNumber(r);
                    var op = b.Op switch
                    {
                        BinaryOp.Add => "+",
                        BinaryOp.Sub => "-",
                        BinaryOp.Mul => "*",
                        BinaryOp.Div => "/",
                        BinaryOp.Mod => "%",
                        _ => throw new InvalidOperationException()
                    };
                    return new Translated($"({ln} {op} {rn})", CSharpKind.Number);
                }
            case BinaryOp.Union:
                throw new UntranslatableXPathException("XPath union ('|') is not yet supported.");
        }
        throw new UntranslatableXPathException($"Unsupported binary op {b.Op}");
    }

    private Translated TranslateComparison(BinaryExpr b, IStatement context)
    {
        var l = Translate(b.Left, context);
        var r = Translate(b.Right, context);

        string op = b.Op switch
        {
            BinaryOp.Eq => "==",
            BinaryOp.Ne => "!=",
            BinaryOp.Lt => "<",
            BinaryOp.Le => "<=",
            BinaryOp.Gt => ">",
            BinaryOp.Ge => ">=",
            _ => throw new InvalidOperationException()
        };

        // If either operand is a string literal/value AND the other is a leaf
        // value/path, compare via string equality after coercion.
        if (b.Op == BinaryOp.Eq || b.Op == BinaryOp.Ne)
        {
            if (l.Kind == CSharpKind.String || r.Kind == CSharpKind.String
                || l.Kind == CSharpKind.LeafValue || r.Kind == CSharpKind.LeafValue
                || l.Kind == CSharpKind.Node || r.Kind == CSharpKind.Node)
            {
                var ls = CoerceToString(l);
                var rs = CoerceToString(r);
                var eq = $"global::System.StringComparer.Ordinal.Equals({ls}, {rs})";
                return new Translated(b.Op == BinaryOp.Eq ? eq : $"!{eq}", CSharpKind.Bool);
            }
        }

        // Numeric comparison fallback.
        var ln = CoerceToNumber(l);
        var rn = CoerceToNumber(r);
        return new Translated($"({ln} {op} {rn})", CSharpKind.Bool);
    }

    private Translated TranslateFunction(FunctionCallExpr fc, IStatement context)
    {
        if (!string.IsNullOrEmpty(fc.Prefix))
        {
            throw new UntranslatableXPathException(
                $"Namespaced function {fc.Prefix}:{fc.Name} is not supported.");
        }
        switch (fc.Name)
        {
            case "true": Expect(fc, 0); return new Translated("true", CSharpKind.Bool);
            case "false": Expect(fc, 0); return new Translated("false", CSharpKind.Bool);
            case "not":
                Expect(fc, 1);
                var inner = Translate(fc.Arguments[0], context);
                return new Translated($"!({CoerceToBool(inner)})", CSharpKind.Bool);
            case "boolean":
                Expect(fc, 1);
                var b = Translate(fc.Arguments[0], context);
                return new Translated(CoerceToBool(b), CSharpKind.Bool);
            case "string":
                Expect(fc, 1);
                var s = Translate(fc.Arguments[0], context);
                return new Translated(CoerceToString(s), CSharpKind.String);
            case "number":
                Expect(fc, 1);
                var nn = Translate(fc.Arguments[0], context);
                return new Translated(CoerceToNumber(nn), CSharpKind.Number);
            case "string-length":
                Expect(fc, 1);
                var sl = Translate(fc.Arguments[0], context);
                return new Translated($"(({CoerceToString(sl)}) ?? string.Empty).Length",
                    CSharpKind.Number);
            case "count":
                Expect(fc, 1);
                return TranslateCount(fc.Arguments[0], context);
            case "current":
                Expect(fc, 0);
                // current() is the context in which the XPath was authored.
                return new Translated(_selfExpression, ClassifyOriginKind(), _origin);
            case "derived-from-or-self":
                Expect(fc, 2);
                return TranslateDerivedFrom(fc, context, includeSelf: true);
            case "derived-from":
                Expect(fc, 2);
                return TranslateDerivedFrom(fc, context, includeSelf: false);
        }
        throw new UntranslatableXPathException($"XPath function '{fc.Name}()' is not yet supported.");
    }

    private CSharpKind ClassifyOriginKind()
    {
        return _origin switch
        {
            Leaf => CSharpKind.LeafValue,
            _ => CSharpKind.Node
        };
    }

    private static void Expect(FunctionCallExpr fc, int arity)
    {
        if (fc.Arguments.Count != arity)
            throw new UntranslatableXPathException(
                $"Function {fc.Name}() expects {arity} argument(s), got {fc.Arguments.Count}.");
    }

    private Translated TranslateCount(XPathExpr arg, IStatement context)
    {
        var t = Translate(arg, context);
        switch (t.Kind)
        {
            case CSharpKind.NodeSet:
                // Works for YangList, List<T>, and arrays via the non-generic
                // ICollection interface.
                return new Translated(
                    $"((double)((({t.Code}) as global::System.Collections.ICollection)?.Count ?? 0))",
                    CSharpKind.Number);
            case CSharpKind.Node:
                return new Translated($"(({t.Code}) is null ? 0d : 1d)", CSharpKind.Number);
            case CSharpKind.LeafValue:
                return new Translated($"(({t.Code}) is null ? 0d : 1d)", CSharpKind.Number);
            default:
                throw new UntranslatableXPathException(
                    $"count() requires a node-set argument; got {t.Kind}.");
        }
    }

    /// <summary>
    /// Translate derived-from(path, 'identity') / derived-from-or-self(path, 'identity').
    /// The identity hierarchy is known at codegen time, so we emit a set-membership check
    /// against all valid enum values.
    /// </summary>
    private Translated TranslateDerivedFrom(FunctionCallExpr fc, IStatement context, bool includeSelf)
    {
        // First arg: the node whose value we're checking.
        var pathTranslated = Translate(fc.Arguments[0], context);

        // Second arg: must be a string literal naming the identity (possibly prefixed).
        if (fc.Arguments[1] is not StringLiteralExpr identityLit)
        {
            throw new UntranslatableXPathException(
                $"{fc.Name}() second argument must be a string literal identity name.");
        }

        var identityName = identityLit.Value;
        var prefix = identityName.Prefix(out var localName);

        // Resolve the identity in the schema.
        var identity = _origin.FindReference<Identity>(identityName);
        if (identity is null)
        {
            throw new UntranslatableXPathException(
                $"Cannot resolve identity '{identityName}' for {fc.Name}().");
        }

        // Collect valid enum values from the identity hierarchy.
        var validValues = Identity.GetInheritanceList(identity).ToArray();
        if (!includeSelf)
        {
            validValues = validValues.Where(v => v != identity).ToArray();
        }

        if (validValues.Length == 0)
        {
            // derived-from with no inheritors is always false.
            return new Translated("false", CSharpKind.Bool);
        }

        // Determine the enum type and its C# namespace.
        var enumClassName = identity.ClassName;
        // Build the fully qualified enum name.
        var module = identity.GetModule() as Module;
        string qualifiedEnum;
        if (module is not null)
        {
            qualifiedEnum = "global::" + Statement.MakeNamespace(module.Argument) + ".YangNode." + enumClassName;
        }
        else
        {
            qualifiedEnum = enumClassName;
        }

        // Emit: (pathExpr == Enum.Value1 || pathExpr == Enum.Value2 || ...)
        // Use a local to avoid repeating the path expression.
        var pathCode = pathTranslated.Code;
        var checks = validValues.Select(v =>
            $"__dfVal == {qualifiedEnum}.{Statement.MakeName(v.Argument)}"
        );
        var checkExpr = string.Join(" || ", checks);
        // Wrap in a block expression via a pattern that the C# compiler handles:
        // We use a lambda immediately invoked, or simpler: just inline comparisons.
        // Since the path expression might be complex, store in a variable via
        // a conditional pattern: ((var __dfVal = pathExpr) is var _ && (checks))
        // Actually simplest: just inline the comparisons directly.
        if (validValues.Length <= 5)
        {
            // Inline OR chain.
            var inlineChecks = validValues.Select(v =>
                $"(object?)({pathCode}) is {qualifiedEnum} __v{v.GetHashCode():X} && __v{v.GetHashCode():X} == {qualifiedEnum}.{Statement.MakeName(v.Argument)}"
            );
            // Simpler: cast to object and compare
            var simpleChecks = validValues.Select(v =>
                $"global::System.Object.Equals(({pathCode}), {qualifiedEnum}.{Statement.MakeName(v.Argument)})"
            );
            return new Translated($"({string.Join(" || ", simpleChecks)})", CSharpKind.Bool);
        }
        else
        {
            // For large hierarchies, emit a switch-style or HashSet. Use a simple OR chain
            // since the compiler will optimize it.
            var simpleChecks = validValues.Select(v =>
                $"global::System.Object.Equals(({pathCode}), {qualifiedEnum}.{Statement.MakeName(v.Argument)})"
            );
            return new Translated($"({string.Join(" || ", simpleChecks)})", CSharpKind.Bool);
        }
    }

    /// <summary>
    /// Translate a location path.
    /// </summary>
    private Translated TranslatePath(PathExpr p, IStatement context)
    {
        if (p.Filter is not null)
        {
            throw new UntranslatableXPathException("Path with filter primary is not yet supported.");
        }

        if (p.IsAbsolute)
        {
            return TranslateAbsolutePath(p, context);
        }

        string code = _selfExpression;
        IStatement? schema = context;
        // 'this' is always a single node instance at runtime, never a node-set,
        // even when the schema is a list (we're inside an entry). For a leaf
        // origin, we expose its value rather than an object reference.
        CSharpKind kind = ClassifyOriginKind();

        foreach (var step in p.Steps)
        {
            var (nextCode, nextSchema, nextKind) = ApplyStep(code, schema, step, kind);
            code = nextCode;
            schema = nextSchema;
            kind = nextKind;
            if (schema is Leaf && step != p.Steps[p.Steps.Count - 1])
            {
                throw new UntranslatableXPathException(
                    "Cannot walk further from a leaf node in the static translator.");
            }
        }

        return new Translated(code, kind, schema);
    }

    /// <summary>
    /// Translate an absolute path (/foo/bar/...) by resolving to the module root
    /// via YangParent walks, then descending.
    /// </summary>
    private Translated TranslateAbsolutePath(PathExpr p, IStatement context)
    {
        var module = _origin.GetModule() as Module;
        if (module is null)
        {
            throw new UntranslatableXPathException(
                "Cannot resolve module for absolute path translation.");
        }

        // Build expression that walks to root.
        var rootCode = EmitRootWalkExpression(context, module);

        // Now apply each step against the module schema.
        string code = rootCode;
        IStatement? schema = (IStatement)module;
        CSharpKind kind = CSharpKind.Node;

        foreach (var step in p.Steps)
        {
            var (nextCode, nextSchema, nextKind) = ApplyStep(code, schema, step, kind);
            code = nextCode;
            schema = nextSchema;
            kind = nextKind;
            if (schema is Leaf && step != p.Steps[p.Steps.Count - 1])
            {
                throw new UntranslatableXPathException(
                    "Cannot walk further from a leaf node in the static translator.");
            }
        }

        return new Translated(code, kind, schema);
    }

    /// <summary>
    /// Emits a C# expression that navigates from the enclosing-this context up to
    /// the module YangNode.
    ///
    /// The generated code structure is:
    ///   YangNode (static-ish class, but it IS the YangParent of top-level containers)
    ///     ├─ InterfacesContainer  (YangParent = YangNode instance)
    ///     │     └─ InterfaceEntry (YangParent = InterfacesContainer)
    ///     ...
    ///
    /// So from a leaf inside InterfaceEntry, we need 2 hops of .YangParent to reach
    /// the YangNode. From InterfacesContainer, 1 hop. From YangNode itself, 0 hops.
    /// </summary>
    private string EmitRootWalkExpression(IStatement context, Module module)
    {
        // For leaf origins, start from the enclosing container.
        var startSchema = context;
        if (startSchema is Leaf or LeafList)
        {
            startSchema = startSchema.Parent;
        }

        // Count how many addressable class-emitting ancestors exist between
        // the startSchema (exclusive of module) and the module.
        int hops = 0;
        var cur = startSchema;
        while (cur is not null && cur != module)
        {
            if (GetGeneratedClassName(cur) is not null && cur is not Module)
            {
                hops++;
            }
            cur = cur.Parent;
        }

        // hops == number of .YangParent calls needed.
        var moduleType = "global::" + Statement.MakeNamespace(module.Argument) + ".YangNode";
        string code = _enclosingThis;
        for (int i = 0; i < hops; i++)
        {
            code = $"(({code}).YangParent)";
        }
        // Final cast to the module type.
        code = $"(({moduleType})({code})!)";
        return code;
    }

    private static CSharpKind ClassifyByTerminalSchema(IStatement? schema) => schema switch
    {
        Leaf => CSharpKind.LeafValue,
        LeafList => CSharpKind.NodeSet,
        List => CSharpKind.NodeSet,
        _ => CSharpKind.Node
    };

    private (string code, IStatement? schema, CSharpKind kind) ApplyStep(
        string code, IStatement? schema, XPathStep step, CSharpKind currentKind)
    {
        if (schema is null)
        {
            throw new UntranslatableXPathException(
                "Step navigation past a null schema context is not supported.");
        }

        // If we're currently a node-set (e.g. stepped into a list without a key
        // predicate, or via a prior LINQ projection), and the next step is a child
        // axis with a name test, we emit LINQ .SelectMany/.Select to project
        // into each entry.
        if (currentKind == CSharpKind.NodeSet && step.Axis == XPathAxis.Child
            && step.Test is NameTest linqNt && linqNt.LocalName != "*")
        {
            if (schema is List listSchema)
            {
                return ApplyLinqChildStep(code, listSchema, step, linqNt.LocalName);
            }
            // The schema might be a Container/Choice etc. from a prior LINQ
            // projection (the enumerable elements are of that type).
            var namedChild = FindNamedChild(schema, linqNt.LocalName);
            if (namedChild is not null)
            {
                return ApplyLinqProjection(code, schema, namedChild, step);
            }
        }

        switch (step.Axis)
        {
            case XPathAxis.Self:
                // Self preserves the current runtime kind (we don't drop into
                // a node-set just because the schema happens to be a List).
                return (code, schema, currentKind);
            case XPathAxis.Parent:
                {
                    var parentSchema = SkipNonAddressable(schema.Parent);
                    if (parentSchema is null)
                    {
                        throw new UntranslatableXPathException(
                            "'..' from a top-level node has no addressable parent.");
                    }
                    if (schema is Leaf or LeafList)
                    {
                        if (step.Predicates.Count > 0)
                        {
                            throw new UntranslatableXPathException(
                                "Predicates on a '..' step are not supported.");
                        }
                        return (_enclosingThis, parentSchema, CSharpKind.Node);
                    }
                    var parentTypeName = Statement.ResolveQualifiedClassName(parentSchema)
                        ?? throw new UntranslatableXPathException("Parent has no generated class to navigate to.");
                    var nextCode = $"(({code}).YangParent)";
                    nextCode = $"(({parentTypeName})({nextCode})!)";
                    if (step.Predicates.Count > 0)
                    {
                        throw new UntranslatableXPathException(
                            "Predicates on a '..' step are not supported.");
                    }
                    return (nextCode, parentSchema, CSharpKind.Node);
                }
            case XPathAxis.Child:
                return ApplyChildStep(code, schema, step);
            case XPathAxis.DescendantOrSelf:
                // descendant-or-self::node() is injected by the parser for '//'.
                // It cannot navigate by itself — it only marks that the *next*
                // step should search recursively. We represent it as a no-op
                // that sets a flag; the parent loop must detect it.
                // However, the parser inserts it as an explicit step before the
                // actual named step. We handle the canonical pattern here:
                // if the test is node(), it's a no-op and we stay where we are.
                if (step.Test is NodeTypeTest nt2 && nt2.TypeName == "node")
                {
                    // Mark that the NEXT step should be descendant-aware.
                    // We cannot represent this in a single return value, so we
                    // use a sentinel schema + special kind.
                    return (code, schema, CSharpKind.NodeSet); // signal: descendant context
                }
                // If it's a named test, treat as "find named descendant" directly.
                if (step.Test is NameTest dnt && dnt.LocalName != "*")
                {
                    return TranslateDescendantNamedStep(code, schema, dnt.LocalName, step.Predicates);
                }
                throw new UntranslatableXPathException(
                    "descendant-or-self axis with wildcard or non-node() test is not yet supported.");
            case XPathAxis.Descendant:
                if (step.Test is NameTest dnt2 && dnt2.LocalName != "*")
                {
                    return TranslateDescendantNamedStep(code, schema, dnt2.LocalName, step.Predicates);
                }
                throw new UntranslatableXPathException(
                    "descendant axis with wildcard test is not yet supported.");
            default:
                throw new UntranslatableXPathException(
                    $"Axis '{step.Axis}' is not yet supported by the translator.");
        }
    }

    private (string code, IStatement? schema, CSharpKind kind) ApplyChildStep(
        string code, IStatement schema, XPathStep step)
    {
        if (step.Test is NodeTypeTest ntt && ntt.TypeName == "node")
        {
            // node() test on child axis — matches all child data nodes.
            // This is unusual in YANG; degrade.
            throw new UntranslatableXPathException(
                "child::node() is not translatable in the static translator.");
        }

        if (step.Test is not NameTest nt)
        {
            throw new UntranslatableXPathException("Only name tests on the child axis are supported.");
        }
        if (nt.LocalName == "*")
        {
            // Wildcard: in a boolean context this is true if any data child exists.
            // We count all direct data children.
            return TranslateWildcardChild(code, schema, step);
        }

        // Look up the named child in the schema. If the step has a prefix,
        // resolve across modules; otherwise search locally + cross-module from root.
        var named = FindNamedChild(schema, nt.LocalName, nt.Prefix, _origin);
        if (named is null)
        {
            throw new UntranslatableXPathException(
                $"Could not resolve child '{nt.LocalName}' on schema node '{schema.Argument}'.");
        }

        // If the found node lives inside a choice/case, prepend the path through
        // the choice and case properties so the C# navigation is correct.
        string navCode = code;
        if (named.Parent is Case cs2 && cs2.Parent is Choice ch2 && ch2.Parent == schema)
        {
            navCode = $"(({code})?.{MakeNameSafe(ch2.Argument)})?.{cs2.TargetName}";
        }
        else if (named.Parent is Choice ch3 && ch3.Parent == schema)
        {
            navCode = $"(({code})?.{MakeNameSafe(ch3.Argument)})";
        }
        else if (schema is List && named.Parent == schema.Parent)
        {
            // Found a sibling of the List (accessible from within an entry via
            // .YangParent which points to the list's owning container).
            var ownerType = Statement.ResolveQualifiedClassName(schema.Parent);
            if (ownerType is not null)
            {
                navCode = $"(({ownerType})(({code}).YangParent)!)";
            }
            else
            {
                navCode = code;
            }
        }
        else
        {
            navCode = code;
        }

        // Cross-module navigation: if the found node lives in a different module
        // than the current schema, navigate via Configuration (the shared root).
        var namedModule = named.GetModule() as Module;
        var schemaModule = schema.GetModule() as Module ?? schema as Module;
        if (namedModule is not null && schemaModule is not null && namedModule != schemaModule)
        {
            // Navigate: currentModule.YangParent (= Configuration) → Configuration.TargetModule
            var configType = _origin.Parent is CompilationUnit cu2
                ? $"global::{cu2.MyNamespace}.Configuration"
                : (GetRoot(_origin)?.Children.OfType<Module>().FirstOrDefault()?.Parent is CompilationUnit cu3
                    ? $"global::{cu3.MyNamespace}.Configuration"
                    : null);
            var targetModuleType = "global::" + Statement.MakeNamespace(namedModule.Argument) + ".YangNode";
            var targetModuleProp = Statement.MakeName(namedModule.Argument);

            if (configType is not null)
            {
                navCode = $"(({configType})(({navCode})?.YangParent)!)?.{targetModuleProp}";
            }
            else
            {
                throw new UntranslatableXPathException(
                    $"Cross-module reference to '{nt.LocalName}' in module '{namedModule.Argument}' " +
                    "cannot be resolved without a Configuration root.");
            }
        }

        string nextCode;
        IStatement nextSchema = named;
        CSharpKind kind;
        switch (named)
        {
            case Leaf leaf:
                nextCode = $"(({navCode})?.{leaf.TargetName})";
                kind = CSharpKind.LeafValue;
                break;
            case Container c:
                nextCode = $"(({navCode})?.{c.TargetName})";
                kind = CSharpKind.Node;
                break;
            case List l:
                if (step.Predicates.Count == 1
                    && TryTranslateKeyPredicate(l, step.Predicates[0], out var keyExpr))
                {
                    nextCode = $"((({navCode})?.{l.TargetName})?[{keyExpr}])";
                    kind = CSharpKind.Node;
                }
                else
                {
                    nextCode = $"(({navCode})?.{l.TargetName})";
                    kind = CSharpKind.NodeSet;
                }
                break;
            case LeafList ll:
                nextCode = $"(({navCode})?.{ll.TargetName})";
                kind = CSharpKind.NodeSet;
                break;
            case Choice ch:
                nextCode = $"(({navCode})?.{MakeNameSafe(ch.Argument)})";
                kind = CSharpKind.Node;
                break;
            case Case cs:
                nextCode = $"(({navCode})?.{cs.TargetName})";
                kind = CSharpKind.Node;
                break;
            default:
                throw new UntranslatableXPathException(
                    $"Cannot step into schema node of type {named.GetType().Name}.");
        }

        // Predicates other than the single key predicate handled above.
        if (named is not List && step.Predicates.Count > 0)
        {
            throw new UntranslatableXPathException(
                "Predicates on a non-list child step are not yet supported.");
        }

        return (nextCode, nextSchema, kind);
    }

    /// <summary>
    /// Wildcard child step: `*` matches all direct data-node children.
    /// We translate this as an existence check (for bool context) or a count
    /// of all non-null children (for numeric context via count(*)).
    /// Emits a sum of null-checks on all leaf/container/list/leaf-list properties.
    /// </summary>
    private (string code, IStatement? schema, CSharpKind kind) TranslateWildcardChild(
        string code, IStatement schema, XPathStep step)
    {
        if (step.Predicates.Count > 0)
        {
            throw new UntranslatableXPathException("Predicates on wildcard child step are not supported.");
        }

        // Collect all direct data-node children from the schema.
        var accessors = new List<string>();
        foreach (var child in schema.Children)
        {
            switch (child)
            {
                case Leaf l: accessors.Add($"{l.TargetName}"); break;
                case Container c: accessors.Add($"{c.TargetName}"); break;
                case List l2: accessors.Add($"{l2.TargetName}"); break;
                case LeafList ll: accessors.Add($"{ll.TargetName}"); break;
            }
        }

        if (accessors.Count == 0)
        {
            // No children — count is always 0, exists is always false.
            return ("0d", schema, CSharpKind.Number);
        }

        // Emit: (double)( (code.A is not null ? 1 : 0) + (code.B is not null ? 1 : 0) + ... )
        var parts = accessors.Select(a => $"((object?)(({code})?.{a}) is not null ? 1 : 0)");
        var sumExpr = $"((double)({string.Join(" + ", parts)}))";
        return (sumExpr, schema, CSharpKind.Number);
    }

    /// <summary>
    /// When the current expression is a node-set (e.g. a List/YangList without a key
    /// predicate), apply a child step via LINQ projection. This allows XPaths like
    /// <c>/networks/network/network-types</c> to iterate all network entries.
    ///
    /// Emits: <c>code?.Select(__e => __e.ChildProp).Where(__x => __x != null)</c>
    /// for container/choice children, or <c>code?.Select(__e => __e.LeafProp)</c>
    /// for leaf children. The result is still a node-set if the child is a list/container,
    /// or a leaf-value-set for leaves.
    /// </summary>
    private (string code, IStatement? schema, CSharpKind kind) ApplyLinqChildStep(
        string code, List listSchema, XPathStep step, string yangName)
    {
        // Find the named child within the list entry's schema.
        var named = FindNamedChild(listSchema, yangName);
        if (named is null)
        {
            throw new UntranslatableXPathException(
                $"Could not resolve child '{yangName}' on list entry '{listSchema.Argument}'.");
        }

        string projection;
        CSharpKind kind;
        switch (named)
        {
            case Leaf leaf:
                projection = $"{code}?.Select(__e => __e.{leaf.TargetName})";
                kind = CSharpKind.NodeSet; // set of leaf values
                break;
            case Container c:
                projection = $"{code}?.Select(__e => __e.{c.TargetName}).Where(__x => __x != null)";
                kind = CSharpKind.NodeSet;
                break;
            case List l:
                projection = $"{code}?.SelectMany(__e => __e.{l.TargetName} ?? global::System.Linq.Enumerable.Empty<{l.ClassName}>())";
                kind = CSharpKind.NodeSet;
                break;
            case LeafList ll:
                projection = $"{code}?.SelectMany(__e => __e.{ll.TargetName} ?? global::System.Array.Empty<{ll.ClassName}>())";
                kind = CSharpKind.NodeSet;
                break;
            case Choice ch:
                projection = $"{code}?.Select(__e => __e.{MakeNameSafe(ch.Argument)}).Where(__x => __x != null)";
                kind = CSharpKind.NodeSet;
                break;
            default:
                throw new UntranslatableXPathException(
                    $"Cannot project into schema node of type {named.GetType().Name} via LINQ.");
        }

        if (step.Predicates.Count > 0)
        {
            throw new UntranslatableXPathException(
                "Predicates on LINQ-projected child steps are not yet supported.");
        }

        return (projection, named, kind);
    }

    /// <summary>
    /// Continues a LINQ chain when the current expression is an IEnumerable of
    /// some container/node type and we want to step into a child property of
    /// each element. Emits .Select(__e => __e.Child).Where(...) or similar.
    /// </summary>
    private (string code, IStatement? schema, CSharpKind kind) ApplyLinqProjection(
        string code, IStatement parentSchema, IStatement namedChild, XPathStep step)
    {
        if (step.Predicates.Count > 0)
        {
            throw new UntranslatableXPathException(
                "Predicates on LINQ-chained projection are not yet supported.");
        }

        string projection;
        CSharpKind kind;
        switch (namedChild)
        {
            case Leaf leaf:
                projection = $"({code})?.Select(__e => __e.{leaf.TargetName})";
                kind = CSharpKind.NodeSet;
                break;
            case Container c:
                projection = $"({code})?.Select(__e => __e.{c.TargetName}).Where(__x => __x != null)";
                kind = CSharpKind.NodeSet;
                break;
            case List l:
                projection = $"({code})?.SelectMany(__e => __e.{l.TargetName} ?? global::System.Linq.Enumerable.Empty<{l.ClassName}>())";
                kind = CSharpKind.NodeSet;
                break;
            case LeafList ll:
                projection = $"({code})?.SelectMany(__e => __e.{ll.TargetName} ?? global::System.Array.Empty<{ll.ClassName}>())";
                kind = CSharpKind.NodeSet;
                break;
            case Choice ch:
                projection = $"({code})?.Select(__e => __e.{MakeNameSafe(ch.Argument)}).Where(__x => __x != null)";
                kind = CSharpKind.NodeSet;
                break;
            default:
                throw new UntranslatableXPathException(
                    $"Cannot LINQ-project into schema node of type {namedChild.GetType().Name}.");
        }
        return (projection, namedChild, kind);
    }

    /// <summary>
    /// Translate a descendant named step: find a child named <paramref name="yangName"/>
    /// anywhere in the subtree under <paramref name="schema"/>. We use a simple
    /// approach: search statically for the first matching descendant in the schema
    /// tree. If there is exactly one, emit a direct navigation path; if there are
    /// multiple or none, throw.
    /// </summary>
    private (string code, IStatement? schema, CSharpKind kind) TranslateDescendantNamedStep(
        string code, IStatement schema, string yangName, IReadOnlyList<XPathExpr> predicates)
    {
        // BFS to find the named descendant.
        var matches = FindDescendants(schema, yangName);
        if (matches.Count == 0)
        {
            throw new UntranslatableXPathException(
                $"Could not find descendant '{yangName}' under '{schema.Argument}'.");
        }
        if (matches.Count > 1)
        {
            throw new UntranslatableXPathException(
                $"Ambiguous descendant '{yangName}' under '{schema.Argument}' ({matches.Count} matches). Cannot statically translate.");
        }

        // Walk down the single path from the root to the match.
        var (path, target) = matches[0];
        string cur = code;
        IStatement? curSchema = schema;
        CSharpKind kind = CSharpKind.Node;
        foreach (var intermediate in path)
        {
            var fakeStep = new XPathStep(XPathAxis.Child,
                new NameTest(null, intermediate.Argument),
                System.Array.Empty<XPathExpr>());
            var (nc, ns, nk) = ApplyChildStep(cur, curSchema!, fakeStep);
            cur = nc;
            curSchema = ns;
            kind = nk;
        }

        // Apply the final step (the target itself).
        var finalStep = new XPathStep(XPathAxis.Child,
            new NameTest(null, target.Argument), predicates);
        var (fc, fs, fk) = ApplyChildStep(cur, curSchema!, finalStep);
        return (fc, fs, fk);
    }

    /// <summary>
    /// BFS to find all descendants with a given YANG name, returning the
    /// path of intermediate containers to reach them.
    /// </summary>
    private static List<(List<IStatement> path, IStatement target)> FindDescendants(
        IStatement root, string yangName)
    {
        var results = new List<(List<IStatement> path, IStatement target)>();
        var queue = new Queue<(IStatement node, List<IStatement> path)>();
        queue.Enqueue((root, new List<IStatement>()));

        while (queue.Count > 0)
        {
            var (node, path) = queue.Dequeue();
            foreach (var child in node.Children)
            {
                if (child is not (Leaf or Container or List or LeafList or Choice or Case))
                    continue;
                if (child.Argument == yangName)
                {
                    results.Add((path, child));
                }
                // Continue searching in containers and list entries (they can have nested matches).
                if (child is Container or List)
                {
                    var childPath = new List<IStatement>(path) { child };
                    queue.Enqueue((child, childPath));
                }
            }
        }
        return results;
    }

    private bool TryTranslateKeyPredicate(List list, XPathExpr predicate, out string keyExpr)
    {
        keyExpr = string.Empty;
        if (!list.HasKey) return false;
        var key = list.GetKey();
        if (key is null) return false;
        var keyFields = key.KeyPropertyNames;
        if (keyFields.Length != 1) return false; // composite-key predicate not yet supported

        // Only translate string-typed keys (most common). For other key types
        // (unions, integers), we'd need type-aware coercion.
        var keyLeaf = list.Children.OfType<Leaf>()
            .FirstOrDefault(l => l.TargetName == keyFields[0]);
        if (keyLeaf is null) return false;
        if (keyLeaf.ClassName != "string") return false;

        // Recognise predicates of the form  key-leaf = literal-or-variable
        if (predicate is not BinaryExpr be || be.Op != BinaryOp.Eq) return false;
        var (lhsField, rhsExpr) = MatchKeyFieldEquality(be, keyFields[0]);
        if (lhsField is null || rhsExpr is null) return false;

        var rhs = Translate(rhsExpr, _origin);
        keyExpr = CoerceToString(rhs);
        return true;
    }

    private static (string? field, XPathExpr? other) MatchKeyFieldEquality(BinaryExpr be, string keyField)
    {
        // Left side is a single name test matching keyField?
        if (IsBareName(be.Left, out var leftName) && leftName.Equals(keyField, StringComparison.OrdinalIgnoreCase))
            return (leftName, be.Right);
        if (IsBareName(be.Right, out var rightName) && rightName.Equals(keyField, StringComparison.OrdinalIgnoreCase))
            return (rightName, be.Left);
        return (null, null);
    }

    private static bool IsBareName(XPathExpr expr, out string name)
    {
        name = string.Empty;
        if (expr is PathExpr pe && !pe.IsAbsolute && pe.Steps.Count == 1
            && pe.Steps[0].Axis == XPathAxis.Child
            && pe.Steps[0].Test is NameTest nt && nt.LocalName != "*"
            && pe.Steps[0].Predicates.Count == 0)
        {
            name = Statement.MakeName(nt.LocalName);
            return true;
        }
        return false;
    }

    private static string MakeNameSafe(string argument) => Statement.MakeName(argument);

    private static IStatement? SkipNonAddressable(IStatement? statement)
    {
        while (statement is not null && GetGeneratedClassName(statement) is null)
        {
            statement = statement.Parent;
        }
        return statement;
    }

    private static IStatement? FindNamedChild(IStatement schema, string yangName,
        string? prefix = null, IStatement? origin = null)
    {
        // If a prefix is given, resolve the target module and search there.
        if (!string.IsNullOrEmpty(prefix) && origin is not null)
        {
            var targetModule = origin.FindSourceFor(prefix!) as Module;
            if (targetModule is not null)
            {
                var found = FindNamedChildLocal(targetModule, yangName);
                if (found is not null) return found;
            }
        }

        // Local search on the current schema node.
        var result = FindNamedChildLocal(schema, yangName);
        if (result is not null) return result;

        // If the schema is a List, also search the List's parent (the owning
        // container). In XPath, the list's entries are children of the list's
        // parent, so siblings of the list are accessible from within an entry
        // after one '..' hop. The schema tree has entries under the List node,
        // but XPath semantics treat them as siblings at the List's parent level.
        if (schema is List && schema.Parent is not null)
        {
            result = FindNamedChildLocal(schema.Parent, yangName);
            if (result is not null) return result;
        }

        // Cross-module search from module level.
        if (schema is Module && origin is not null)
        {
            var root = GetRoot(origin);
            if (root is not null)
            {
                foreach (var sibling in root.Children.OfType<Module>())
                {
                    if (sibling == schema) continue;
                    var found = FindNamedChildLocal(sibling, yangName);
                    if (found is not null) return found;
                }
            }
        }

        return null;
    }

    private static IStatement? FindNamedChildLocal(IStatement schema, string yangName)
    {
        // Look for a direct child matching the YANG name.
        foreach (var child in schema.Children)
        {
            switch (child)
            {
                case Leaf l when l.Argument == yangName: return l;
                case Container c when c.Argument == yangName: return c;
                case List ls when ls.Argument == yangName: return ls;
                case LeafList ll when ll.Argument == yangName: return ll;
                case Choice ch when ch.Argument == yangName: return ch;
                case Case cs when cs.Argument == yangName: return cs;
            }
        }
        // XPath treats choices as transparent.
        foreach (var child in schema.Children)
        {
            if (child is Choice ch)
            {
                foreach (var caseChild in ch.Children)
                {
                    if (caseChild is Case cs)
                    {
                        var found = FindNamedChildLocal(cs, yangName);
                        if (found is not null) return found;
                    }
                    else
                    {
                        switch (caseChild)
                        {
                            case Leaf l when l.Argument == yangName: return l;
                            case Container c2 when c2.Argument == yangName: return c2;
                            case List ls when ls.Argument == yangName: return ls;
                            case LeafList ll when ll.Argument == yangName: return ll;
                        }
                    }
                }
            }
        }
        return null;
    }

    private static IStatement? GetRoot(IStatement statement)
    {
        while (statement.Parent is not null)
        {
            statement = statement.Parent;
        }
        return statement;
    }

    private static string? GetGeneratedClassName(IStatement statement) => statement switch
    {
        Container c => c.ClassName,
        List l => l.ClassName,
        Choice ch => ch.ClassName,
        Case cs => cs.ClassName,
        Input i => i.ClassName,
        Output o => o.ClassName,
        Notification n => n.ClassName,
        Module => "YangNode",
        _ => null
    };

    private static bool NeedsCast(IStatement child, IStatement parent)
    {
        // We always emit a cast for parent navigation because the static type
        // of YangParent might be Object? or a less-derived type in unusual
        // generator paths; the explicit cast is harmless when redundant.
        return true;
    }

    private static string CoerceToBool(Translated t)
    {
        return t.Kind switch
        {
            CSharpKind.Bool => t.Code,
            CSharpKind.Number => $"({t.Code} != 0d)",
            CSharpKind.String => $"!string.IsNullOrEmpty({t.Code})",
            // Wrap in (object?) so the null check works whether the value is a
            // class, a Nullable<T>, or a non-nullable struct (e.g. an enum).
            CSharpKind.Node => $"((object?)({t.Code}) is not null)",
            // YangList<,>, List<>, and T[] all implement ICollection, which
            // exposes Count without needing a static element type.
            CSharpKind.NodeSet => $"((({t.Code}) as global::System.Collections.ICollection)?.Count > 0)",
            CSharpKind.LeafValue => $"((object?)({t.Code}) is not null)",
            _ => throw new UntranslatableXPathException($"Cannot coerce {t.Kind} to bool.")
        };
    }

    private static string CoerceToNumber(Translated t)
    {
        return t.Kind switch
        {
            CSharpKind.Number => t.Code,
            CSharpKind.Bool => $"({t.Code} ? 1d : 0d)",
            CSharpKind.String => $"double.Parse(({t.Code}) ?? \"NaN\", global::System.Globalization.CultureInfo.InvariantCulture)",
            CSharpKind.LeafValue => $"global::System.Convert.ToDouble((object?)({t.Code}) ?? double.NaN, global::System.Globalization.CultureInfo.InvariantCulture)",
            CSharpKind.NodeSet => $"((double)((({t.Code}) as global::System.Collections.ICollection)?.Count ?? 0))",
            CSharpKind.Node => $"((object?)({t.Code}) is null ? 0d : 1d)",
            _ => throw new UntranslatableXPathException($"Cannot coerce {t.Kind} to number.")
        };
    }

    private static string CoerceToString(Translated t)
    {
        return t.Kind switch
        {
            CSharpKind.String => t.Code,
            CSharpKind.Number => $"({t.Code}).ToString(global::System.Globalization.CultureInfo.InvariantCulture)",
            CSharpKind.Bool => $"(({t.Code}) ? \"true\" : \"false\")",
            // Box first so .ToString() works for both reference and (nullable
            // or non-nullable) value types.
            CSharpKind.LeafValue => $"((object?)({t.Code}))?.ToString()",
            CSharpKind.Node => $"((object?)({t.Code}))?.ToString()",
            // Node-set string value = string value of the first node.
            CSharpKind.NodeSet => $"((({t.Code}) as global::System.Collections.IEnumerable)?.Cast<object?>().FirstOrDefault()?.ToString())",
            _ => throw new UntranslatableXPathException($"Cannot coerce {t.Kind} to string.")
        };
    }

    private static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
