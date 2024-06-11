using System.Xml;

namespace YangSupport;

public static class SerializationHelper
{
    public static XmlWriterSettings GetStandardWriterSettings()
    {
        return new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true,
            NewLineOnAttributes = false,
            Async = true
        };
    }

    public static XmlReaderSettings GetStandardReaderSettings()
    {
        return new XmlReaderSettings
        {
            Async = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            IgnoreWhitespace = true,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };
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

    public static async Task SerializeRegularExceptionAsync(this Stream output, Exception exception, string? id)
    {
        using var exceptionWriter = XmlWriter.Create(output, GetStandardWriterSettings());
        await exceptionWriter.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
        if (id != null)
        {
            await exceptionWriter.WriteAttributeStringAsync(null, "message-id", null, id);
        }

        await exceptionWriter.WriteStartElementAsync(null, "rpc-error", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await exceptionWriter.WriteStartElementAsync(null, "error-type", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await exceptionWriter.WriteStringAsync("application");
        await exceptionWriter.WriteEndElementAsync();
        await exceptionWriter.WriteStartElementAsync(null, "error-tag", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await exceptionWriter.WriteStringAsync("unknown");
        await exceptionWriter.WriteEndElementAsync();
        await exceptionWriter.WriteStartElementAsync(null, "error-severity", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await exceptionWriter.WriteStringAsync("error");
        await exceptionWriter.WriteEndElementAsync();
        await exceptionWriter.WriteStartElementAsync(null, "error-app-tag", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await exceptionWriter.WriteStringAsync(exception.GetType().Name);
        await exceptionWriter.WriteEndElementAsync();
        if (exception.Message != null)
        {
            await exceptionWriter.WriteStartElementAsync(null, "error-message",
                "urn:ietf:params:xml:ns:netconf:base:1.0");
            await exceptionWriter.WriteStringAsync(exception.Message);
            await exceptionWriter.WriteEndElementAsync();
        }

        await exceptionWriter.WriteEndElementAsync();
        await exceptionWriter.WriteEndElementAsync();
    }

    public static async Task<DateTime> ParseEventTime(this XmlReader reader)
    {
        if (reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:notification:1.0")
        {
            throw new RpcException(ErrorType.Rpc, "wrong-namespace", Severity.Error,
                message:
                "Tried to parse eventTime for an element not in 'urn:ietf:params:xml:ns:netconf:notification:1.0'",
                info: new()
                {
                    ["namespace"] = reader.NamespaceURI,
                    ["element-name"] = reader.Name
                });
        }

        await reader.ReadAsync();
        if (reader.Name != "eventTime")
        {
            throw new RpcException(ErrorType.Rpc, "wrong-element", Severity.Error,
                message:
                "Expected an <eventTime> element when parsing event Time",
                info: new()
                {
                    ["namespace"] = reader.NamespaceURI,
                    ["element-name"] = reader.Name
                });
        }

        await reader.ReadAsync();
        var value = await reader.GetValueAsync();
        if (!DateTime.TryParse(value, out var eventTime))
        {
            throw new RpcException(ErrorType.Rpc, "wrong-element", Severity.Error,
                message:
                "Expected an <eventTime> element to contain a valid datetime, but the parsing failed",
                info: new()
                {
                    ["value"] = value,
                });
        }

        await reader.ReadAsync();
        if (reader.NodeType != XmlNodeType.EndElement)
        {
            throw new RpcException(ErrorType.Rpc, "wrong-element", Severity.Error,
                message:
                "Expected an <eventTime> element to only have a single child, but did not find the end element immediately after parsing",
                info: new()
                {
                    ["namespace"] = reader.NamespaceURI,
                    ["element-name"] = reader.Name
                });
        }

        return eventTime;
    }

    public static string ParseMessageId(this XmlReader reader)
    {
        if (reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0")
        {
            throw new Exception(
                $"Got an <rpc> element with namespace {reader.NamespaceURI}, expected \"urn:ietf:params:xml:ns:netconf:base:1.0\"");
        }

        var id = reader["message-id"];
        if (id is null)
        {
            throw new RpcException(ErrorType.Rpc, "missing-attribute", Severity.Error,
                info: new Dictionary<string, string>
                {
                    ["bad-attribute"] = "message-id", ["bad-element"] = "rpc"
                });
        }

        return id;
    }
}