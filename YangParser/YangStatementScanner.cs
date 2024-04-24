using System;
using System.Collections.Generic;

namespace YangParser;

public class Metadata(string source, Position position, int length)
{
    public string Source { get; } = source;
    public Position Position { get; } = position;
    public int Length { get; } = length;
}

public class YangStatement(
    string prefix,
    string keyword,
    List<YangStatement> children,
    Metadata metadata,
    object? argument = null)
{
    public string Prefix { get; } = prefix;
    public string Keyword { get; } = keyword;
    public object? Argument { get; } = argument;
    public List<YangStatement> Children { get; } = children;
    public Metadata Metadata { get; } = metadata;
}

public class SyntaxError(string sourceRef, string message, Token token)
    : Exception($"{token.Position.Line}:{token.Position.Column}:{message}")
{
    public string SourceRef { get; set; } = sourceRef;
    public Token Token { get; set; } = token;
}

public class ParserState
{
    public readonly string Source;
    private TokenScanner Scanner { get; }
    private Token _lookAhead;

    public TermSymbol NextSymbol => _lookAhead.Symbol;
    public Token LookAhead => _lookAhead;

    public ParserState(string source, string text)
    {
        Scanner = new TokenScanner(text);
        Source = source;
        _lookAhead = Scanner.NextToken();
    }

    public Token Match(TermSymbol symbol)
    {
        if (_lookAhead.Symbol != symbol)
        {
            throw Fail($"Expected a {symbol} but got {_lookAhead.Symbol}");
        }

        var token = _lookAhead;
        _lookAhead = Scanner.NextToken();
        return token;
    }

    public SyntaxError Fail(string message)
    {
        return new SyntaxError(Source, message, LookAhead);
    }
}

public static class Parser
{
    public static YangStatement Parse(string sourceRef, string text)
    {
        var state = new ParserState(sourceRef, text);
        return ParseStatement(state);
    }

    private static YangStatement ParseStatement(ParserState state)
    {
        var (prefix, keyword, keywordPosition) = ParseIdentifier(state);
        var argument = ParseOptionalArgument(state);
        var (children, endPosition) = ParseOptStatementBody(state);

        var length = endPosition.Offset - keywordPosition.Offset;

        return new YangStatement(prefix, keyword, children, new Metadata(state.Source, keywordPosition, length), argument);
    }

    private static (string Prf, string Kw, Position KwPosition) ParseIdentifier(ParserState state)
    {
        var position = state.LookAhead.Position;

        if (state.NextSymbol == TermSymbol.Identifier)
        {
            var keyword = state.LookAhead.Lexeme;
            state.Match(state.NextSymbol);
            return ("", keyword, position);
        }

        if (state.NextSymbol == TermSymbol.IdentifierRef)
        {
            var splitLexeme = state.LookAhead.Lexeme.Split(':');
            var prefix = splitLexeme[0];
            var keyword = splitLexeme[1];
            state.Match(TermSymbol.IdentifierRef);
            return (prefix, keyword, position);
        }

        throw state.Fail($"Expected an identifier, but got {state.NextSymbol}");
    }

    private static string? ParseOptionalArgument(ParserState state)
    {
        // TODO: Handle string fiddling and alignment according to RFC!
        if (state.NextSymbol == TermSymbol.String)
        {
            var str = (state.Match(TermSymbol.String).Value as string)!;
            var strings = new List<string> { str };

            while (state.LookAhead.Symbol == TermSymbol.StringConcat)
            {
                str = (state.Match(TermSymbol.String).Value as string)!;
                state.Match(TermSymbol.StringConcat);
                strings.Add(str);
            }

            return string.Join("", strings);
        }

        switch (state.NextSymbol)
        {
            case TermSymbol.Identifier:
            case TermSymbol.IdentifierRef:
                return state.Match(state.NextSymbol).Lexeme;
        }

        return null;
    }

    private static (List<YangStatement> Substmts, Position EndPosition) ParseOptStatementBody(ParserState state)
    {
        if (state.NextSymbol == TermSymbol.StatementTerminator)
        {
            var position = state.LookAhead.Position;
            state.Match(TermSymbol.StatementTerminator);
            return (new List<YangStatement>(), position);
        }

        if (state.NextSymbol == TermSymbol.LCurly)
        {
            return ParseStatementBody(state);
        }

        throw state.Fail($"Expected a statement terminator or body, but got {state.NextSymbol}");
    }

    private static (List<YangStatement> Substmts, Position EndPosition) ParseStatementBody(ParserState state)
    {
        var children = new List<YangStatement>();

        state.Match(TermSymbol.LCurly);
        while (state.NextSymbol != TermSymbol.RCurly)
        {
            children.Add(ParseStatement(state));
        }

        var endPosition = state.LookAhead.Position;
        state.Match(TermSymbol.RCurly);

        return (children, endPosition);
    }
}