using System;
using System.Text;

namespace YangParser;

public class TokenScanner(string text)
{
// An efficient version of input.substring(offset, offset + str.length) === str
    private static int MatchString(string input, int offset, string str)
    {
        var matchLength = 0;
        while (offset < input.Length && matchLength < str.Length)
        {
            if (input[offset] != str[matchLength])
                return 0;
            ++offset;
            ++matchLength;
        }

        return str.Length == matchLength ? str.Length : 0;
    }

    private static Func<string, int, int> IsString(string str)
    {
        return (input, offset) => MatchString(input, offset, str);
    }

    private static Func<string, int, int> IsCodepointInRange(int lowerCodepoint, int upperCodepoint)
    {
        return (input, offset) =>
        {
            var cp = input[offset];
            return lowerCodepoint <= cp && cp <= upperCodepoint ? 1 : 0;
        };
    }

    private static Func<string, int, int> IsOneOfStrings(params string[] strings)
    {
        return (input, offset) =>
        {
            foreach (var str in strings)
            {
                var matchLength = MatchString(input, offset, str);
                if (matchLength > 0)
                    return matchLength;
            }

            return 0;
        };
    }

    private static Func<string, int, int> IsAny(params Func<string, int, int>[] predicates)
    {
        return (input, offset) =>
        {
            foreach (var predicate in
                     predicates)
            {
                var match = predicate(input, offset);
                if (match > 0)
                    return match;
            }

            return 0;
        };
    }

    private static readonly Func<string, int, int> IsWs = IsOneOfStrings(" ", "\t", NewLine.ToString(), "\r");
    private static readonly Func<string, int, int> IsNl = IsString("\n");
    private static readonly Func<string, int, int> IsLineComment = IsString("//");
    private static readonly Func<string, int, int> IsBlockCommentStart = IsString("/*");
    private static readonly Func<string, int, int> IsBlockCommendEnd = IsString("*/");
    private static readonly Func<string, int, int> IsStringConcat = IsString("+");
    private static readonly Func<string, int, int> IsSingleQuote = IsString("'");
    private static readonly Func<string, int, int> IsDoubleQuote = IsString('"'.ToString());

    private static readonly Func<string, int, int> IsAlpha = IsAny(IsCodepointInRange(0x41, 0x5A),
        IsCodepointInRange(0x61, 0x7A)); // A-Z and a-z

    private static readonly Func<string, int, int> IsDigit = IsCodepointInRange(0x30, 0x39);
    private static Func<string, int, int> _isMinus = IsString("-");
    private static Func<string, int, int> _isDecimalSeparator = IsString(".");
    private static readonly Func<string, int, int> IsLeftCurly = IsString("{");
    private static readonly Func<string, int, int> IsRightCurly = IsString("}");
    private static readonly Func<string, int, int> IsIdentifierStartChar = IsAny(IsAlpha, IsString("_"));

    private static readonly Func<string, int, int> IsIdentifierChar =
        IsAny(IsIdentifierStartChar, IsDigit, IsString("."), IsString("-"));

    private static readonly Func<string, int, int> IsRefSeparator = IsString(":");
    private static readonly Func<string, int, int> IsStatementTerminator = IsString(";");

    private static readonly Func<string, int, int> IsPunctuatorCharacter =
        IsAny(IsWs, IsLineComment, IsBlockCommentStart, IsStatementTerminator, IsLeftCurly);

    private int _column = 1;
    private int _line = 1;

    private int _offset;

    public Token NextToken()
    {
        while (!IsEof)
        {
            Position position = new Position(_column, _line, _offset);

            if (Currently(IsInvalidYangChar))
            {
                SkipWhile(IsInvalidYangChar);
                return GetToken(TermSymbol.Unknown, position);
            }

            if (Currently(IsWs))
            {
                SkipWhile(IsWs);
                continue;
            }

            if (Currently(IsLineComment))
            {
                SkipUntil(IsNl);
                continue;
            }

            if (Currently(IsBlockCommentStart))
            {
                // Skip start of block comments, so that input such as '/*/' won't trip us up!
                Advance(2); // Skip '/*'
                SkipUntil(IsBlockCommendEnd);
                continue;
            }

            if (Currently(IsStringConcat))
            {
                Advance();
                return GetToken(TermSymbol.StringConcat, position);
            }

            if (Currently(IsLeftCurly))
            {
                Advance();
                return GetToken(TermSymbol.LCurly, position);
            }

            if (Currently(IsRightCurly))
            {
                Advance();
                return GetToken(TermSymbol.RCurly, position);
            }

            if (Currently(IsStatementTerminator))
            {
                Advance();
                return GetToken(TermSymbol.StatementTerminator, position);
            }

            if (Currently(IsSingleQuote))
            {
                Advance(); // Skip "'"...

                // ... to find the ending "'"
                if (!SkipUntil(IsSingleQuote))
                {
                    // Beware of EOF!
                    return GetToken(TermSymbol.Unknown, position);
                }

                return this.GetStringToken(position, false);
            }

            if (Currently(IsDoubleQuote))
            {
                Advance(); // Skip '"'...

                // ... to find the ending "'" (that is not escaped)
                if (!SkipUntilUnescaped(IsDoubleQuote))
                {
                    // Beware of EOF!
                    return GetToken(TermSymbol.Unknown, position);
                }

                return this.GetStringToken(position, true);
            }

            if (Currently(IsIdentifierStartChar))
            {
                while (Currently(IsIdentifierChar))
                {
                    Advance();
                }

                var isRef = false;
                if (Currently(IsRefSeparator))
                {
                    Advance();
                    isRef = true;

                    if (!Currently(IsIdentifierStartChar))
                    {
                        return GetToken(TermSymbol.Unknown, position);
                    }

                    while (!IsEof && !Currently(IsPunctuatorCharacter))
                    {
                        Advance();
                    }
                }
                else
                {
                    while (!IsEof && !Currently(IsPunctuatorCharacter))
                    {
                        Advance();
                    }
                }

                return GetToken(
                    isRef ? TermSymbol.IdentifierRef : TermSymbol.Identifier,
                    position
                );
            }

            // Grab everything else, and call it a string...
            while (!IsEof && !Currently(IsPunctuatorCharacter))
            {
                Advance();
            }

            return GetToken(TermSymbol.String, position, text.Substring(position.Offset, _offset - position.Offset));
        }

        return GetToken(
            TermSymbol.EndOfFile, new Position(_column, _line, _offset));
    }

    public static int IsInvalidYangChar(string str, int offset)
    {
        var input = str[offset];
        return (input == 0x09 ||
                input == 0x0a ||
                input == 0x0d ||
                input >= 0x20 && input <= 0xd7ff ||
                input >= 0xe000 && input <= 0xfdcf ||
                input >= 0xfdf0 && input <= 0xffff)
            ? 0
            : 1;
    }

    private bool Currently(Func<string, int, int> predicate)
    {
        return !IsEof && predicate(text, _offset) > 0;
    }

    private bool SkipWhile(Func<string, int, int> predicate)
    {
        var match = 0;
        while (!IsEof && (match = predicate(text, _offset)) != 0)
        {
            Advance(match);
        }

        return !IsEof;
    }

    private bool SkipUntil(Func<string, int, int> predicate)
    {
        var match = 0;
        while (!IsEof && (match = predicate(text, _offset)) == 0)
        {
            Advance();
        }

        if (!IsEof)
        {
            Advance(match);
            return true;
        }

        return false;
    }

    private const char EscapeChar = '\\';
    private const char NewLine = '\n';

    private bool SkipUntilUnescaped(Func<string, int, int> predicate)
    {
        var match = 0;
        var isEscaping = false;
        while (!IsEof)
        {
            if (isEscaping)
            {
                isEscaping = false;
            }
            else
            {
                if (text[_offset] == EscapeChar)
                {
                    isEscaping = true;
                }
                else
                {
                    if ((match = predicate(text, _offset)) != 0)
                    {
                        break;
                    }
                }
            }

            Advance();
        }

        if (!IsEof)
        {
            Advance(match);
            return true;
        }

        return false;
    }

    private bool IsEof => _offset >= text.Length;


    private Token GetToken(TermSymbol symbol, Position position, object? value = null)
    {
        var length = _offset - position.Offset;
        return new Token(symbol, position, text.Substring(position.Offset, length), length, value);
    }

    private Token GetStringToken(Position position, bool unescape)
    {
        var rawValue = text.Substring(position.Offset + 1, _offset - 1 - (position.Offset + 1));

        // Handle string layouting according to RFC 7950, 6.1.3 Quoting

        // Start by chopping up all lines
        var lines = rawValue.Split('\n');

        // The indent of the string is the column of the first character in the string
        // which is the quote's column + 1 (quote's column = start of the string)
        StringBuilder indenter = new StringBuilder();
        for (int i = 0; i < position.Column + 1; i++)
        {
            indenter.Append(" ");
        }

        var indent = indenter.ToString();

        // We only do layouting for the second and following lines (the first line
        // is the reference!)
        for (var i = 1; i < lines.Length; ++i)
        {
            if (lines[i].StartsWith(indent))
            {
                lines[i] = lines[i].Substring(indent.Length);
            }
        }

        // And stitch the lines together again to form the entire string.
        var value = string.Join("\n", lines);

        return GetToken(TermSymbol.String, position, unescape ? UnescapeString(value) : value);
    }

    private void Advance(int amount = 1)
    {
        for (var i = 0; i < amount && _offset < text.Length; ++i)
        {
            if (text[_offset] == NewLine)
            {
                ++_line;
                _column = 1;
            }
            else
            {
                ++_column;
            }

            ++_offset;
        }
    }

    private string UnescapeString(string str)
    {
        return str.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}