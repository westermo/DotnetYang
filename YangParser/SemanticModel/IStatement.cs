namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; }
    ChildRule[] PermittedChildren { get; }
    IStatement[] Children { get; }
    IStatement? Parent { get; set; }
}