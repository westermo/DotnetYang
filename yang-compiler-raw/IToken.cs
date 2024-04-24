namespace Yang.Compiler;

public interface IToken
{
    string Name { get; }
    IToken? Parent { get; }
}