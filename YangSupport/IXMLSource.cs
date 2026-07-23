using System.Threading.Tasks;
using System.Xml;

namespace YangSupport;

public interface IXMLSource
{
    string ToXML();
}

/// <summary>
/// Interface for YANG nodes that can serialize themselves to XML.
/// Generated container/list-entry classes implement this.
/// </summary>
public interface IYangXmlSerializable
{
    /// <summary>
    /// Serialize this node to XML.
    /// </summary>
    /// <param name="writer">The XML writer to write to.</param>
    /// <param name="configOnly">When true, only config-true data is serialized (for edit-config).</param>
    Task WriteXMLAsync(XmlWriter writer, bool configOnly = false);
}
