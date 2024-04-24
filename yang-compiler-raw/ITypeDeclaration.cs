namespace Yang.Compiler;

public interface ITypeDeclaration : IToken
{
    IType Type { get; }
}