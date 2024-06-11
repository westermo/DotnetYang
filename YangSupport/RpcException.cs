using System.Xml;

namespace YangSupport;

public class RpcException(
    ErrorType type,
    string tag,
    Severity severity,
    string? appTag = null,
    string? xpath = null,
    string? message = null,
    Dictionary<string, string>? info = null) : Exception(message)
{
    public ErrorType Type { get; } = type;
    public string Tag { get; } = tag;
    public Severity Severity { get; } = severity;
    public string? ApplicationTag { get; } = appTag;
    public string? XPath { get; } = xpath;
    public Dictionary<string, string>? Info = info;

    public static async Task<RpcException> ParseAsync(XmlReader reader)
    {
        ErrorType type = default!;
        string tag = default!;
        Severity severity = default!;
        string? applicationTag = default!;
        string? xPath = default!;
        string? message = default!;
        Dictionary<string, string>? info = default!;

        while (await reader.ReadAsync())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.Name)
                    {
                        case "error-type":
                        {
                            await reader.ReadAsync();
                            var strValue = await reader.GetValueAsync();
                            type = strValue switch
                            {
                                "transport" => ErrorType.Transport,
                                "rpc" => ErrorType.Rpc,
                                "protocol" => ErrorType.Protocol,
                                "application" => ErrorType.Application,
                                _ => throw new Exception("Unexpected error-type '" + strValue + "'")
                            };
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-tag":
                        {
                            await reader.ReadAsync();
                            tag = await reader.GetValueAsync();
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-severity":
                        {
                            await reader.ReadAsync();
                            var strValue = await reader.GetValueAsync();
                            severity = strValue switch
                            {
                                "error" => Severity.Error,
                                "warning" => Severity.Warning,
                                _ => throw new Exception("Unexpected severity '" + strValue + "'")
                            };
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-app-tag":
                        {
                            await reader.ReadAsync();
                            applicationTag = await reader.GetValueAsync();
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-path":
                        {
                            await reader.ReadAsync();
                            xPath = await reader.GetValueAsync();
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-message":
                        {
                            await reader.ReadAsync();
                            message = await reader.GetValueAsync();
                            await reader.ReadAsync();
                            continue;
                        }
                        case "error-info":
                        {
                            info = new();
                            string? key = null;
                            string? value = null;
                            while (await reader.ReadAsync())
                            {
                                if (reader.Name == "error-info") break;
                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    key ??= reader.Name;
                                }

                                if (reader.NodeType == XmlNodeType.EndElement)
                                {
                                    if (key is not null && value is not null)
                                        info[key] = value;
                                    key = null;
                                    value = null;
                                }

                                if (reader.NodeType == XmlNodeType.Text)
                                {
                                    value ??= string.Empty;
                                    value += await reader.GetValueAsync();
                                }
                            }

                            continue;
                        }
                    }

                    break;
                case XmlNodeType.EndElement when reader.Name == "rpc-error":
                    await reader.ReadAsync();
                    return new RpcException(type, tag, severity, applicationTag, xPath, message, info);
                default:
                    throw new Exception(
                        $"Unexpected node type '{reader.NodeType}' : '{reader.Name}' under 'rpc-error'");
            }
        }

        throw new Exception("Couldn't parse RPC error");
    }

    public async Task SerializeAsync(Stream output, string? id)
    {
        using var writer = XmlWriter.Create(output, SerializationHelper.GetStandardWriterSettings());
        await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
        if (id != null)
        {
            await writer.WriteAttributeStringAsync(null, "message-id", null, id);
        }

        await writer.WriteStartElementAsync(null, "rpc-error", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteStartElementAsync(null, "error-type", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteStringAsync(Type switch
        {
            ErrorType.Transport => "transport",
            ErrorType.Rpc => "rpc",
            ErrorType.Protocol => "protocol",
            ErrorType.Application => "application",
            _ => "unknown"
        });
        await writer.WriteEndElementAsync();
        await writer.WriteStartElementAsync(null, "error-tag", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteStringAsync(Tag);
        await writer.WriteEndElementAsync();
        await writer.WriteStartElementAsync(null, "error-severity", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteStringAsync(Severity switch
        {
            Severity.Error => "error",
            Severity.Warning => "warning",
            _ => "unknown"
        });
        await writer.WriteEndElementAsync();
        if (ApplicationTag != null)
        {
            await writer.WriteStartElementAsync(null, "error-app-tag",
                "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteStringAsync(ApplicationTag);
            await writer.WriteEndElementAsync();
        }

        if (XPath != null)
        {
            await writer.WriteStartElementAsync(null, "error-path", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteStringAsync(XPath);
            await writer.WriteEndElementAsync();
        }

        if (Message != null)
        {
            await writer.WriteStartElementAsync(null, "error-message",
                "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteStringAsync(Message);
            await writer.WriteEndElementAsync();
        }

        if (Info != null)
        {
            await writer.WriteStartElementAsync(null, "error-info", "urn:ietf:params:xml:ns:netconf:base:1.0");
            foreach (var info in Info)
            {
                await writer.WriteStartElementAsync(null, info.Key, "urn:ietf:params:xml:ns:netconf:base:1.0");
                await writer.WriteStringAsync(info.Value);
                await writer.WriteEndElementAsync();
            }

            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndElementAsync();
    }
}