namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; }
    ChildRule[] PermittedChildren { get; }
    IStatement[] Children { get; }
    IStatement? Parent { get; set; }
}

public interface IFunctionSource : IStatement;

public interface IClassSource : IStatement;

public interface IAttributeSource : IStatement
{
    string AttributeName { get; }
    bool Active { get; }
}