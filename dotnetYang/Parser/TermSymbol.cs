namespace YangParser.Parser;

public enum TermSymbol
{
    LCurly,
    RCurly,
    Identifier,
    IdentifierRef,
    StatementTerminator,
    String,
    StringConcat,
    Unknown,
    EndOfFile
}