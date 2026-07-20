// XPath 1.0 lexer used by the YANG -> C# source generator.
//
// Only ASCII-friendly identifiers are supported; YANG identifiers are
// constrained to the ASCII letter/digit/dash/underscore set anyway.

using System;
using System.Collections.Generic;
using System.Text;

namespace YangParser.SemanticModel.XPath;

internal enum TokenKind
{
    Eof,
    Number,
    Literal,           // string literal
    Name,              // QName: optional prefix + local name
    Wildcard,          // *
    LParen,
    RParen,
    LBracket,
    RBracket,
    Slash,
    DoubleSlash,
    Dot,
    DoubleDot,
    At,                // @ (attribute axis abbreviation)
    Comma,
    DoubleColon,       // ::
    Pipe,              // | (union)
    Plus,
    Minus,
    Equal,
    NotEqual,
    LessThan,
    LessOrEqual,
    GreaterThan,
    GreaterOrEqual,
    DollarSign,        // $variable
    Dollar,            // alias (kept for clarity)
    // Multi-character operator names that look like NAME but appear
    // only in operator position: 'and', 'or', 'mod', 'div'.
    OperatorAnd,
    OperatorOr,
    OperatorMod,
    OperatorDiv
}

internal struct Token
{
    public TokenKind Kind;
    public string Text;
    public int Position;

    public override string ToString() => $"{Kind}({Text})";
}

internal sealed class XPathLexer
{
    private readonly string _input;
    private int _pos;

    public XPathLexer(string input)
    {
        _input = input ?? string.Empty;
        _pos = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            SkipWhitespace();
            if (_pos >= _input.Length)
            {
                tokens.Add(new Token { Kind = TokenKind.Eof, Position = _pos, Text = string.Empty });
                return tokens;
            }
            var t = NextToken(tokens);
            if (t.Kind == TokenKind.Name || t.Kind == TokenKind.Wildcard)
            {
                // Per XPath 1.0 disambiguation rule: 'and'/'or'/'mod'/'div' are
                // operator names only when the preceding token is not @, ::, (, [, ,
                // or an operator. Otherwise they are NCNames.
                if (t.Kind == TokenKind.Name && OperatorContext(tokens))
                {
                    var op = t.Text switch
                    {
                        "and" => TokenKind.OperatorAnd,
                        "or" => TokenKind.OperatorOr,
                        "mod" => TokenKind.OperatorMod,
                        "div" => TokenKind.OperatorDiv,
                        _ => (TokenKind?)null
                    };
                    if (op is not null) t.Kind = op.Value;
                }
            }
            tokens.Add(t);
        }
    }

    private static bool OperatorContext(List<Token> previousTokens)
    {
        if (previousTokens.Count == 0) return false;
        var prev = previousTokens[previousTokens.Count - 1].Kind;
        return prev switch
        {
            TokenKind.Name or TokenKind.Number or TokenKind.Literal or TokenKind.RParen or TokenKind.RBracket or
            TokenKind.Wildcard or TokenKind.Dot or TokenKind.DoubleDot => true,
            _ => false
        };
    }

    private Token NextToken(List<Token> previous)
    {
        var start = _pos;
        var c = _input[_pos];

        switch (c)
        {
            case '(': _pos++; return new Token { Kind = TokenKind.LParen, Text = "(", Position = start };
            case ')': _pos++; return new Token { Kind = TokenKind.RParen, Text = ")", Position = start };
            case '[': _pos++; return new Token { Kind = TokenKind.LBracket, Text = "[", Position = start };
            case ']': _pos++; return new Token { Kind = TokenKind.RBracket, Text = "]", Position = start };
            case ',': _pos++; return new Token { Kind = TokenKind.Comma, Text = ",", Position = start };
            case '|': _pos++; return new Token { Kind = TokenKind.Pipe, Text = "|", Position = start };
            case '@': _pos++; return new Token { Kind = TokenKind.At, Text = "@", Position = start };
            case '+': _pos++; return new Token { Kind = TokenKind.Plus, Text = "+", Position = start };
            case '-':
                _pos++;
                return new Token { Kind = TokenKind.Minus, Text = "-", Position = start };
            case '$':
                _pos++;
                return new Token { Kind = TokenKind.DollarSign, Text = "$", Position = start };
            case '=': _pos++; return new Token { Kind = TokenKind.Equal, Text = "=", Position = start };
            case '!':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '=')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.NotEqual, Text = "!=", Position = start };
                }
                throw new XPathParseException("Unexpected '!' (expected '!=')", start);
            case '<':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '=')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.LessOrEqual, Text = "<=", Position = start };
                }
                _pos++;
                return new Token { Kind = TokenKind.LessThan, Text = "<", Position = start };
            case '>':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '=')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.GreaterOrEqual, Text = ">=", Position = start };
                }
                _pos++;
                return new Token { Kind = TokenKind.GreaterThan, Text = ">", Position = start };
            case '/':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '/')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.DoubleSlash, Text = "//", Position = start };
                }
                _pos++;
                return new Token { Kind = TokenKind.Slash, Text = "/", Position = start };
            case '.':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == '.')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.DoubleDot, Text = "..", Position = start };
                }
                if (_pos + 1 < _input.Length && _input[_pos + 1] >= '0' && _input[_pos + 1] <= '9')
                {
                    return ReadNumber(start);
                }
                _pos++;
                return new Token { Kind = TokenKind.Dot, Text = ".", Position = start };
            case ':':
                if (_pos + 1 < _input.Length && _input[_pos + 1] == ':')
                {
                    _pos += 2;
                    return new Token { Kind = TokenKind.DoubleColon, Text = "::", Position = start };
                }
                throw new XPathParseException("Stray ':'", start);
            case '*':
                _pos++;
                return new Token { Kind = TokenKind.Wildcard, Text = "*", Position = start };
            case '"':
            case '\'':
                return ReadStringLiteral(start, c);
        }

        if (IsDigit(c)) return ReadNumber(start);
        if (IsNameStart(c)) return ReadName(start);

        throw new XPathParseException($"Unexpected character '{c}'", start);
    }

    private Token ReadStringLiteral(int start, char quote)
    {
        _pos++; // consume opening quote
        var sb = new StringBuilder();
        while (_pos < _input.Length && _input[_pos] != quote)
        {
            sb.Append(_input[_pos]);
            _pos++;
        }
        if (_pos >= _input.Length)
        {
            throw new XPathParseException("Unterminated string literal", start);
        }
        _pos++; // closing quote
        return new Token { Kind = TokenKind.Literal, Text = sb.ToString(), Position = start };
    }

    private Token ReadNumber(int start)
    {
        var sb = new StringBuilder();
        while (_pos < _input.Length && IsDigit(_input[_pos]))
        {
            sb.Append(_input[_pos]);
            _pos++;
        }
        if (_pos < _input.Length && _input[_pos] == '.')
        {
            sb.Append('.');
            _pos++;
            while (_pos < _input.Length && IsDigit(_input[_pos]))
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
        }
        return new Token { Kind = TokenKind.Number, Text = sb.ToString(), Position = start };
    }

    private Token ReadName(int start)
    {
        var sb = new StringBuilder();
        while (_pos < _input.Length && IsNameChar(_input[_pos]))
        {
            sb.Append(_input[_pos]);
            _pos++;
        }
        // QName: prefix:localName
        if (_pos < _input.Length && _input[_pos] == ':'
            && _pos + 1 < _input.Length && _input[_pos + 1] != ':'
            && IsNameStart(_input[_pos + 1]))
        {
            sb.Append(':');
            _pos++;
            while (_pos < _input.Length && IsNameChar(_input[_pos]))
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
        }
        // QName wildcard: prefix:*
        else if (_pos < _input.Length && _input[_pos] == ':'
                 && _pos + 1 < _input.Length && _input[_pos + 1] == '*')
        {
            sb.Append(':');
            _pos++;
            sb.Append('*');
            _pos++;
        }
        return new Token { Kind = TokenKind.Name, Text = sb.ToString(), Position = start };
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && IsWhitespace(_input[_pos])) _pos++;
    }

    private static bool IsWhitespace(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsNameStart(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    private static bool IsNameChar(char c) => IsNameStart(c) || IsDigit(c) || c == '-' || c == '.';
}

internal sealed class XPathParseException : Exception
{
    public XPathParseException(string message, int position)
        : base($"XPath parse error at position {position}: {message}")
    {
        Position = position;
    }
    public int Position { get; }
}
