namespace YangParser;

public readonly struct Token(TermSymbol symbol, Position position, string lexeme, int length, object? value = null)
{
    public readonly TermSymbol Symbol = symbol;
    public readonly Position Position = position;
    public readonly string Lexeme = lexeme;
    public readonly int Length = length;
    public readonly object? Value = value;
}