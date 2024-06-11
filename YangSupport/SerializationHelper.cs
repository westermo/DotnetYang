using System.Xml;

namespace YangSupport;

public static class SerializationHelper
{
    public static XmlWriterSettings GetStandardWriterSettings()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.OmitXmlDeclaration = true;
        settings.NewLineOnAttributes = false;
        settings.Async = true;
        return settings;
    }

    public static XmlReaderSettings GetStandardReaderSettings()
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.Async = true;
        settings.ConformanceLevel = ConformanceLevel.Fragment;
        settings.IgnoreWhitespace = true;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        return settings;
    }

    public static async Task ExpectOkRpcReply(XmlReader reader, int messageID)
    {
        await reader.ReadAsync();
        if (reader.NodeType != XmlNodeType.Element || reader.Name != "rpc-reply" ||
            reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0" ||
            reader["message-id"] != messageID.ToString())
        {
            throw new Exception(
                $"Expected stream to start with a <rpc-reply> element with message id {messageID} & \"urn:ietf:params:xml:ns:netconf:base:1.0\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
        }

        await reader.ReadAsync();
        if (reader.NodeType != XmlNodeType.Element || reader.Name != "ok")
        {
            if (reader.Name == "rpc-error") throw await RpcException.ParseAsync(reader);
            throw new Exception($"Expected <ok/> element {reader.NodeType}: {reader.Name}");
        }

        await reader.ReadAsync();
        if (reader.NodeType != XmlNodeType.EndElement)
        {
            throw new Exception($"Expected </rpc-reply> closing element {reader.NodeType}: {reader.Name}");
        }
    }
}