using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Netconf;

/// <summary>
/// XML helpers shared by the NETCONF client and notification subscription.
/// </summary>
internal static class NetconfXml
{
    public const string BaseNamespace = "urn:ietf:params:xml:ns:netconf:base:1.0";

    /// <summary>Namespace manager mapping the "nc" prefix to the NETCONF base namespace.</summary>
    public static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
    {
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("nc", BaseNamespace);
        return nsMgr;
    }

    /// <summary>Wraps <paramref name="filter"/> in a subtree &lt;filter&gt; element, or returns "" when null.</summary>
    public static string SubtreeFilter(string? filter)
    {
        return filter != null ? $"<filter type=\"subtree\">{filter}</filter>" : "";
    }

    /// <summary>
    /// Parses an XML fragment with <paramref name="parseFunc"/>, with the reader positioned on the first node.
    /// </summary>
    public static async Task<T> ParseFragmentAsync<T>(string xml, Func<XmlReader, Task<T>> parseFunc)
    {
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync().ConfigureAwait(false);
        return await parseFunc(reader).ConfigureAwait(false);
    }
}
