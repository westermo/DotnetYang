// XPath 1.0 recursive-descent parser.
//
// Follows the W3C XPath 1.0 grammar (subset relevant to YANG when/must):
//
//   OrExpr ::= AndExpr ('or' AndExpr)*
//   AndExpr ::= EqExpr ('and' EqExpr)*
//   EqExpr ::= RelExpr (('='|'!=') RelExpr)*
//   RelExpr ::= AddExpr (('<'|'<='|'>'|'>=') AddExpr)*
//   AddExpr ::= MulExpr (('+'|'-') MulExpr)*
//   MulExpr ::= UnaryExpr (('*'|'div'|'mod') UnaryExpr)*
//   UnaryExpr ::= '-' UnaryExpr | UnionExpr
//   UnionExpr ::= PathExpr ('|' PathExpr)*
//   PathExpr ::= LocationPath
//              | FilterExpr (('/'|'//') RelativeLocationPath)?
//   FilterExpr ::= PrimaryExpr Predicate*
//   PrimaryExpr ::= VariableReference | '(' Expr ')' | Literal | Number | FunctionCall
//   LocationPath ::= RelativeLocationPath | AbsoluteLocationPath
//   AbsoluteLocationPath ::= '/' RelativeLocationPath? | '//' RelativeLocationPath
//   RelativeLocationPath ::= Step (('/'|'//') Step)*
//   Step ::= AxisSpecifier NodeTest Predicate* | AbbreviatedStep
//   AbbreviatedStep ::= '.' | '..'

using System.Collections.Generic;

namespace YangParser.SemanticModel.XPath;

internal sealed class XPathParser
{
    private readonly string _source;
    private readonly List<Token> _tokens;
    private int _index;

    public XPathParser(string source)
    {
        _source = source ?? string.Empty;
        _tokens = new XPathLexer(_source).Tokenize();
    }

    public static XPathExpr Parse(string source) => new XPathParser(source).ParseExpr();

    public XPathExpr ParseExpr()
    {
        var expr = ParseOrExpr();
        Expect(TokenKind.Eof);
        expr.SourceText = _source;
        return expr;
    }

    private XPathExpr ParseOrExpr()
    {
        var left = ParseAndExpr();
        while (Match(TokenKind.OperatorOr))
        {
            var right = ParseAndExpr();
            left = new BinaryExpr(BinaryOp.Or, left, right);
        }
        return left;
    }

    private XPathExpr ParseAndExpr()
    {
        var left = ParseEqExpr();
        while (Match(TokenKind.OperatorAnd))
        {
            var right = ParseEqExpr();
            left = new BinaryExpr(BinaryOp.And, left, right);
        }
        return left;
    }

    private XPathExpr ParseEqExpr()
    {
        var left = ParseRelExpr();
        while (true)
        {
            BinaryOp? op = Peek().Kind switch
            {
                TokenKind.Equal => BinaryOp.Eq,
                TokenKind.NotEqual => BinaryOp.Ne,
                _ => null
            };
            if (op is null) break;
            _index++;
            var right = ParseRelExpr();
            left = new BinaryExpr(op.Value, left, right);
        }
        return left;
    }

    private XPathExpr ParseRelExpr()
    {
        var left = ParseAddExpr();
        while (true)
        {
            BinaryOp? op = Peek().Kind switch
            {
                TokenKind.LessThan => BinaryOp.Lt,
                TokenKind.LessOrEqual => BinaryOp.Le,
                TokenKind.GreaterThan => BinaryOp.Gt,
                TokenKind.GreaterOrEqual => BinaryOp.Ge,
                _ => null
            };
            if (op is null) break;
            _index++;
            var right = ParseAddExpr();
            left = new BinaryExpr(op.Value, left, right);
        }
        return left;
    }

    private XPathExpr ParseAddExpr()
    {
        var left = ParseMulExpr();
        while (true)
        {
            BinaryOp? op = Peek().Kind switch
            {
                TokenKind.Plus => BinaryOp.Add,
                TokenKind.Minus => BinaryOp.Sub,
                _ => null
            };
            if (op is null) break;
            _index++;
            var right = ParseMulExpr();
            left = new BinaryExpr(op.Value, left, right);
        }
        return left;
    }

    private XPathExpr ParseMulExpr()
    {
        var left = ParseUnaryExpr();
        while (true)
        {
            BinaryOp? op = Peek().Kind switch
            {
                TokenKind.Wildcard => BinaryOp.Mul,
                TokenKind.OperatorDiv => BinaryOp.Div,
                TokenKind.OperatorMod => BinaryOp.Mod,
                _ => null
            };
            if (op is null) break;
            // Disambiguation: '*' is multiplication only in operator context.
            // We rely on the lexer's contextual rewrite for and/or/mod/div; '*'
            // stays as Wildcard, so check if previous token is operator-context.
            if (op == BinaryOp.Mul && !IsAfterOperand()) break;
            _index++;
            var right = ParseUnaryExpr();
            left = new BinaryExpr(op.Value, left, right);
        }
        return left;
    }

    private bool IsAfterOperand()
    {
        // We've already consumed the operand for `left`; safer check is whether
        // ParseUnaryExpr would otherwise consume '*' as the start of a step. If
        // _index points at '*' and the previous token was a name/number/closing
        // bracket/parenthesis/dot, we treat it as multiplication.
        if (_index == 0) return false;
        var prev = _tokens[_index - 1].Kind;
        return prev is TokenKind.Name or TokenKind.Number or TokenKind.RParen
            or TokenKind.RBracket or TokenKind.Dot or TokenKind.DoubleDot
            or TokenKind.Literal;
    }

    private XPathExpr ParseUnaryExpr()
    {
        if (Match(TokenKind.Minus))
        {
            return new UnaryMinusExpr(ParseUnaryExpr());
        }
        return ParseUnionExpr();
    }

    private XPathExpr ParseUnionExpr()
    {
        var left = ParsePathExpr();
        while (Match(TokenKind.Pipe))
        {
            var right = ParsePathExpr();
            left = new BinaryExpr(BinaryOp.Union, left, right);
        }
        return left;
    }

    private XPathExpr ParsePathExpr()
    {
        // If the next token starts a primary expression that is not a
        // node-test, parse a FilterExpr (PrimaryExpr Predicate*) and
        // optionally follow with /Step|//Step.
        if (LooksLikeFilterExprStart())
        {
            var primary = ParsePrimaryExpr();
            var preds = ParsePredicates();
            var filterExpr = preds.Count == 0 ? primary : new FilterExpr(primary, preds);

            if (Peek().Kind == TokenKind.Slash || Peek().Kind == TokenKind.DoubleSlash)
            {
                var steps = new List<XPathStep>();
                while (Peek().Kind == TokenKind.Slash || Peek().Kind == TokenKind.DoubleSlash)
                {
                    if (Match(TokenKind.DoubleSlash))
                    {
                        steps.Add(DescendantOrSelfNode());
                    }
                    else
                    {
                        _index++;
                    }
                    steps.Add(ParseStep());
                }
                return new PathExpr(isAbsolute: false, filter: filterExpr, steps: steps);
            }
            return filterExpr;
        }
        return ParseLocationPath();
    }

    private bool LooksLikeFilterExprStart()
    {
        var t = Peek();
        switch (t.Kind)
        {
            case TokenKind.LParen:
            case TokenKind.Literal:
            case TokenKind.Number:
            case TokenKind.DollarSign:
                return true;
            case TokenKind.Name:
                // Function call iff '(' follows AND name is not a NodeType keyword.
                return _index + 1 < _tokens.Count
                       && _tokens[_index + 1].Kind == TokenKind.LParen
                       && !IsNodeTypeKeyword(t.Text);
        }
        return false;
    }

    private static bool IsNodeTypeKeyword(string name)
    {
        return name == "comment" || name == "text" || name == "processing-instruction" || name == "node";
    }

    private XPathExpr ParseLocationPath()
    {
        // AbsoluteLocationPath
        if (Match(TokenKind.Slash))
        {
            // "/" alone is the document root selector.
            if (IsStepStart())
            {
                var steps = new List<XPathStep> { ParseStep() };
                while (Peek().Kind == TokenKind.Slash || Peek().Kind == TokenKind.DoubleSlash)
                {
                    if (Match(TokenKind.DoubleSlash)) steps.Add(DescendantOrSelfNode());
                    else _index++;
                    steps.Add(ParseStep());
                }
                return new PathExpr(isAbsolute: true, filter: null, steps: steps);
            }
            return new PathExpr(isAbsolute: true, filter: null, steps: new List<XPathStep>());
        }
        if (Match(TokenKind.DoubleSlash))
        {
            var steps = new List<XPathStep> { DescendantOrSelfNode(), ParseStep() };
            while (Peek().Kind == TokenKind.Slash || Peek().Kind == TokenKind.DoubleSlash)
            {
                if (Match(TokenKind.DoubleSlash)) steps.Add(DescendantOrSelfNode());
                else _index++;
                steps.Add(ParseStep());
            }
            return new PathExpr(isAbsolute: true, filter: null, steps: steps);
        }
        // RelativeLocationPath
        var rel = new List<XPathStep> { ParseStep() };
        while (Peek().Kind == TokenKind.Slash || Peek().Kind == TokenKind.DoubleSlash)
        {
            if (Match(TokenKind.DoubleSlash)) rel.Add(DescendantOrSelfNode());
            else _index++;
            rel.Add(ParseStep());
        }
        return new PathExpr(isAbsolute: false, filter: null, steps: rel);
    }

    private bool IsStepStart()
    {
        var t = Peek().Kind;
        return t is TokenKind.Name or TokenKind.Wildcard or TokenKind.Dot or TokenKind.DoubleDot or TokenKind.At;
    }

    private XPathStep DescendantOrSelfNode()
    {
        return new XPathStep(XPathAxis.DescendantOrSelf,
            new NodeTypeTest("node"), new List<XPathExpr>());
    }

    private XPathStep ParseStep()
    {
        // AbbreviatedStep
        if (Match(TokenKind.Dot))
        {
            return new XPathStep(XPathAxis.Self, new NodeTypeTest("node"), new List<XPathExpr>());
        }
        if (Match(TokenKind.DoubleDot))
        {
            return new XPathStep(XPathAxis.Parent, new NodeTypeTest("node"), new List<XPathExpr>());
        }

        var axis = XPathAxis.Child;
        if (Match(TokenKind.At))
        {
            axis = XPathAxis.Attribute;
        }
        else if (Peek().Kind == TokenKind.Name && _index + 1 < _tokens.Count
                 && _tokens[_index + 1].Kind == TokenKind.DoubleColon)
        {
            axis = ParseAxisName(Peek().Text);
            _index += 2;
        }

        var test = ParseNodeTest();
        var predicates = ParsePredicates();
        return new XPathStep(axis, test, predicates);
    }

    private static XPathAxis ParseAxisName(string name) => name switch
    {
        "child" => XPathAxis.Child,
        "parent" => XPathAxis.Parent,
        "self" => XPathAxis.Self,
        "descendant" => XPathAxis.Descendant,
        "descendant-or-self" => XPathAxis.DescendantOrSelf,
        "ancestor" => XPathAxis.Ancestor,
        "ancestor-or-self" => XPathAxis.AncestorOrSelf,
        "following" => XPathAxis.Following,
        "following-sibling" => XPathAxis.FollowingSibling,
        "preceding" => XPathAxis.Preceding,
        "preceding-sibling" => XPathAxis.PrecedingSibling,
        "attribute" => XPathAxis.Attribute,
        "namespace" => XPathAxis.Namespace,
        _ => throw new XPathParseException($"Unknown axis '{name}'", 0)
    };

    private NodeTest ParseNodeTest()
    {
        if (Peek().Kind == TokenKind.Name && IsNodeTypeKeyword(Peek().Text)
            && _index + 1 < _tokens.Count && _tokens[_index + 1].Kind == TokenKind.LParen)
        {
            var typeName = Peek().Text;
            _index++;
            Expect(TokenKind.LParen);
            string? literal = null;
            if (typeName == "processing-instruction" && Peek().Kind == TokenKind.Literal)
            {
                literal = Peek().Text;
                _index++;
            }
            Expect(TokenKind.RParen);
            return new NodeTypeTest(typeName, literal);
        }
        if (Match(TokenKind.Wildcard))
        {
            return new NameTest(null, "*");
        }
        if (Peek().Kind == TokenKind.Name)
        {
            var qn = Peek().Text;
            _index++;
            return SplitQName(qn);
        }
        throw new XPathParseException($"Expected node test, got {Peek().Kind}", Peek().Position);
    }

    private static NameTest SplitQName(string qn)
    {
        var colon = qn.IndexOf(':');
        if (colon < 0) return new NameTest(null, qn);
        return new NameTest(qn.Substring(0, colon), qn.Substring(colon + 1));
    }

    private List<XPathExpr> ParsePredicates()
    {
        var list = new List<XPathExpr>();
        while (Peek().Kind == TokenKind.LBracket)
        {
            _index++;
            var expr = ParseOrExpr();
            Expect(TokenKind.RBracket);
            list.Add(expr);
        }
        return list;
    }

    private XPathExpr ParsePrimaryExpr()
    {
        var t = Peek();
        switch (t.Kind)
        {
            case TokenKind.LParen:
                _index++;
                var inner = ParseOrExpr();
                Expect(TokenKind.RParen);
                return new ParenExpr(inner);
            case TokenKind.Literal:
                _index++;
                return new StringLiteralExpr(t.Text);
            case TokenKind.Number:
                _index++;
                return new NumberLiteralExpr(double.Parse(t.Text, System.Globalization.CultureInfo.InvariantCulture));
            case TokenKind.DollarSign:
                _index++;
                if (Peek().Kind != TokenKind.Name)
                    throw new XPathParseException("Expected variable name after '$'", Peek().Position);
                var v = Peek().Text;
                _index++;
                var (vp, vn) = SplitQNameTuple(v);
                return new VariableRefExpr(vp, vn);
            case TokenKind.Name:
                // FunctionCall: Name '(' ... ')'
                var fname = t.Text;
                _index++;
                Expect(TokenKind.LParen);
                var args = new List<XPathExpr>();
                if (Peek().Kind != TokenKind.RParen)
                {
                    args.Add(ParseOrExpr());
                    while (Match(TokenKind.Comma)) args.Add(ParseOrExpr());
                }
                Expect(TokenKind.RParen);
                var (fp, fn) = SplitQNameTuple(fname);
                return new FunctionCallExpr(fp, fn, args);
        }
        throw new XPathParseException($"Unexpected token {t.Kind}", t.Position);
    }

    private static (string? prefix, string name) SplitQNameTuple(string qn)
    {
        var colon = qn.IndexOf(':');
        if (colon < 0) return (null, qn);
        return (qn.Substring(0, colon), qn.Substring(colon + 1));
    }

    private Token Peek() => _tokens[_index];

    private bool Match(TokenKind kind)
    {
        if (_tokens[_index].Kind == kind)
        {
            _index++;
            return true;
        }
        return false;
    }

    private void Expect(TokenKind kind)
    {
        if (_tokens[_index].Kind != kind)
        {
            throw new XPathParseException($"Expected {kind} but got {_tokens[_index].Kind}", _tokens[_index].Position);
        }
        _index++;
    }
}
