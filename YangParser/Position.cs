namespace YangParser;

public readonly struct Position(int column, int line, int offset)
{
    public readonly int Column = column;
    public readonly int Line = line;
    public readonly int Offset = offset;
}