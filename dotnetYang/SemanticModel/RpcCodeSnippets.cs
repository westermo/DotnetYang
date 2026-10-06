namespace YangParser.SemanticModel;

/// <summary>
/// Generated-code fragments shared by RPC and action emission (client call and server dispatch).
/// </summary>
internal static class RpcCodeSnippets
{
    private const string BaseNamespace = "urn:ietf:params:xml:ns:netconf:base:1.0";

    /// <summary>Opens the writer and the &lt;rpc message-id="..."&gt; envelope on the channel.</summary>
    public const string OpenRpcEnvelope =
        $$"""
          using XmlWriter writer = XmlWriter.Create(channel.WriteStream, SerializationHelper.GetStandardWriterSettings());
          await writer.WriteStartElementAsync(null,"rpc","{{BaseNamespace}}");
          await writer.WriteAttributeStringAsync(null,"message-id",null,messageID.ToString());
          """;

    /// <summary>
    /// Reads the &lt;rpc-reply&gt; from the channel. When <paramref name="outputType"/> is set the
    /// reply is parsed and returned as that type, otherwise an &lt;ok/&gt; reply is expected.
    /// </summary>
    public static string ReadReply(string? outputType) => outputType is not null
        ? $$"""
            using XmlReader reader = XmlReader.Create(channel.ReadStream, SerializationHelper.GetStandardReaderSettings());
            await reader.ReadAsync();
            if(reader.NodeType != XmlNodeType.Element || reader.Name != "rpc-reply" || reader.NamespaceURI != "{{BaseNamespace}}" || reader["message-id"] != messageID.ToString())
            {
                throw new Exception($"Expected stream to start with a <rpc-reply> element with message id {messageID} & \"{{BaseNamespace}}\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
            }
            var value = await {{outputType}}.ParseAsync(reader);
            return value;
            """
        : """
          using XmlReader reader = XmlReader.Create(channel.ReadStream, SerializationHelper.GetStandardReaderSettings());
          await SerializationHelper.ExpectOkRpcReply(reader, messageID);
          """;

    /// <summary>
    /// Server side: awaits the handler <c>task</c> and writes its output, or &lt;ok/&gt; when the
    /// operation has no output.
    /// </summary>
    public static string WriteServerResponse(bool hasOutput) => hasOutput
        ? """
          var response = await task;
          await response.WriteXMLAsync(writer);
          """
        : $$"""
            await task;
            await writer.WriteStartElementAsync(null,"ok","{{BaseNamespace}}");
            await writer.WriteEndElementAsync();
            """;
}
