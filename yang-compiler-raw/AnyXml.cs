namespace Yang.Compiler;

public class AnyXml(IToken parent) : IToken
{
    public string Name => "anyxml";
    public IToken? Parent { get; } = parent;
}