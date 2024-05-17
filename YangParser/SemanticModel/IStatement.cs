using System.Collections.Generic;

namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; }
    ChildRule[] PermittedChildren { get; }
    IStatement[] Children { get; }
    IStatement? Parent { get; set; }
}

public interface ICommentable : IStatement
{
    List<string> Comments { get; }
}
public interface IFunctionSource : ICommentable;

public interface IClassSource : ICommentable;

public interface ICommentSource : IStatement;

public interface IAttributeSource : IStatement
{
    string AttributeName { get; }
    bool Active { get; }
}