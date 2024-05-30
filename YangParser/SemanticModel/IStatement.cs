using System.Collections.Generic;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public interface IStatement
{
    string Argument { get; set; }
    ChildRule[] PermittedChildren { get; }
    HashSet<string> Attributes { get; }
    HashSet<string> Keywords { get; }
    IStatement[] Children { get; set; }
    IStatement? Parent { get; set; }
    string AttributeString { get; }
    string DescriptionString { get; }
    Metadata Metadata { get; }
    YangStatement Source { get; }
    string ToCode();
    void Replace(IStatement child, IEnumerable<IStatement> replacements);
    void Insert(IEnumerable<IStatement> augments);
    string XPath { get; }
    (string Namespace, string Prefix)? XmlNamespace { get; set; }
    string Prefix { get; }
}

/// <summary>
/// Implies this class makes an XElement with potential children
/// </summary>
public interface IXMLSource : IStatement
{
    string TargetName { get; }
}
/// <summary>
/// Implies this class makes a singular XElement with no children, and may not have a method
/// </summary>
public interface IXMLValue : IStatement
{
    string TargetName { get; }
    string WriteCall { get; }
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

public interface IUnexpandable : IStatement;