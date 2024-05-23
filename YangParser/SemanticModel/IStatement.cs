using System.Collections.Generic;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; set;  }
    ChildRule[] PermittedChildren { get; }
    HashSet<string> Attributes { get; }
    HashSet<string> Keywords { get; }
    IStatement[] Children { get; set; }
    IStatement? Parent { get; set; }
    Metadata Metadata { get; }
    YangStatement Source { get; }
    string ToCode();
    void Replace(IStatement child, IEnumerable<IStatement> replacements);
}

public interface ICommentable : IStatement
{
    List<string> Comments { get; }
}

public interface IFunctionSource : ICommentable;

public interface IClassSource : ICommentable;

public interface ICommentSource : IStatement;

public interface IPropertySource : IStatement
{
    string Type { get; }
}

public interface IAttributeSource : IStatement
{
    string AttributeName { get; }
    bool Active { get; }
}