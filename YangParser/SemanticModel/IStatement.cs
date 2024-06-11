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
    string Namespace { get; }
    string XmlObjectName { get; }
}

public interface IXMLElement : IStatement
{
    string? TargetName { get; }
}

/// <summary>
/// Implies this class makes an XElement with potential children
/// </summary>
public interface IXMLSource : IXMLElement
{
}

public interface IXMLParseable : IXMLSource
{
    string ClassName { get; }
}

/// <summary>
/// Implies this class makes a singular XElement with no children, and may not have a method
/// </summary>
public interface IXMLWriteValue : IXMLElement
{
    string WriteCall { get; }
}

/// <summary>
/// Implies this class makes a singular XElement with no children, and may not have a method
/// </summary>
public interface IXMLReadValue : IXMLElement
{
    string ClassName { get; }
    string ParseCall { get; }
}

public interface IXMLAction : IXMLElement
{
    string ParseCall { get; }
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