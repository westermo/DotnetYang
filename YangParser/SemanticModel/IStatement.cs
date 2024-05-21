using System.Collections.Generic;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; }
    Dictionary<string, IStatement> GroupingDictionary { get; }
    ChildRule[] PermittedChildren { get; }
    List<string> Attributes { get; }
    List<string> Keywords { get; }
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