using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Netconf;

/// <summary>
/// High-level NETCONF client that provides typed operations over a transport stream.
/// Handles hello exchange, capability negotiation, message framing (base:1.0 and 1.1),
/// and message-id tracking.
///
/// Usage:
///   var client = await NetconfClient.ConnectAsync(inputStream, outputStream);
///   var config = await client.GetConfig();
///   await client.EditConfig(myNode, configOnly: true);
///   await client.CloseSession();
/// </summary>
public class NetconfClient : IDisposable
{
    private readonly Stream _input;
    private readonly Stream _output;
    private int _messageId;
    private bool _disposed;

    /// <summary>Session information including negotiated capabilities.</summary>
    public NetconfSessionInfo Session { get; } = new();

    private NetconfClient(Stream input, Stream output)
    {
        _input = input;
        _output = output;
    }

    /// <summary>
    /// Create a NETCONF client over the given transport streams and perform the hello exchange.
    /// The streams should be connected to a NETCONF server's SSH subsystem channel.
    /// </summary>
    public static async Task<NetconfClient> ConnectAsync(
        Stream input, Stream output, CancellationToken ct = default)
    {
        var client = new NetconfClient(input, output);
        await client.ExchangeHelloAsync(ct).ConfigureAwait(false);
        return client;
    }

    private async Task ExchangeHelloAsync(CancellationToken ct)
    {
        // Read server hello (always EOM framed initially)
        var serverHello = await NetconfFraming.ReadEomMessageAsync(_input, ct).ConfigureAwait(false);
        ParseServerHello(serverHello);

        // Send client hello
        var clientHello = BuildClientHello();
        var encoded = NetconfFraming.EncodeEom(clientHello);
        await _output.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
        await _output.FlushAsync().ConfigureAwait(false);
    }

    private void ParseServerHello(string hello)
    {
        var doc = new XmlDocument();
        doc.LoadXml(hello);
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");

        var sessionIdNode = doc.SelectSingleNode("//nc:session-id", nsMgr);
        if (sessionIdNode != null && int.TryParse(sessionIdNode.InnerText, out var sid))
            Session.SessionId = sid;

        var caps = doc.SelectNodes("//nc:capability", nsMgr);
        if (caps == null) return;

        foreach (XmlNode cap in caps)
        {
            var uri = cap.InnerText?.Trim() ?? "";
            Session.ServerCapabilities.Add(uri);

            if (uri.Contains("base:1.1")) Session.Base11 = true;
            if (uri.Contains("writable-running")) Session.WritableRunning = true;
            if (uri.Contains("candidate")) Session.Candidate = true;
            if (uri.Contains("confirmed-commit")) Session.ConfirmedCommit = true;
            if (uri.Contains("rollback-on-error")) Session.RollbackOnError = true;
            if (uri.Contains("validate")) Session.Validate = true;
            if (uri.Contains("startup")) Session.Startup = true;
            if (uri.Contains("xpath")) Session.Xpath = true;
        }
    }

    private string BuildClientHello()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <hello xmlns="urn:ietf:params:xml:ns:netconf:base:1.0">
              <capabilities>
                <capability>urn:ietf:params:netconf:base:1.0</capability>
                <capability>urn:ietf:params:netconf:base:1.1</capability>
              </capabilities>
            </hello>
            """;
    }

    private int NextMessageId() => Interlocked.Increment(ref _messageId);

    /// <summary>
    /// Send an RPC and return the parsed reply XML.
    /// Handles framing negotiation (base:1.0 vs 1.1).
    /// </summary>
    public async Task<XmlDocument> SendRpcAsync(string rpcContent, CancellationToken ct = default)
    {
        var msgId = NextMessageId();
        var rpc = $"""
            <rpc xmlns="urn:ietf:params:xml:ns:netconf:base:1.0" message-id="{msgId}">
              {rpcContent}
            </rpc>
            """;

        // Encode and send
        byte[] encoded;
        if (Session.Base11)
            encoded = NetconfFraming.EncodeChunked(rpc);
        else
            encoded = NetconfFraming.EncodeEom(rpc);

        await _output.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
        await _output.FlushAsync().ConfigureAwait(false);

        // Read reply
        string reply;
        if (Session.Base11)
            reply = await NetconfFraming.ReadChunkedMessageAsync(_input, ct).ConfigureAwait(false);
        else
            reply = await NetconfFraming.ReadEomMessageAsync(_input, ct).ConfigureAwait(false);

        var doc = new XmlDocument();
        doc.LoadXml(reply);

        // Check for rpc-error
        var nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        var errorNode = doc.SelectSingleNode("//nc:rpc-error", nsMgr);
        if (errorNode != null)
        {
            var errorType = doc.SelectSingleNode("//nc:rpc-error/nc:error-type", nsMgr)?.InnerText ?? "application";
            var errorTag = doc.SelectSingleNode("//nc:rpc-error/nc:error-tag", nsMgr)?.InnerText ?? "operation-failed";
            var errorSeverity = doc.SelectSingleNode("//nc:rpc-error/nc:error-severity", nsMgr)?.InnerText ?? "error";
            var errorMessage = doc.SelectSingleNode("//nc:rpc-error/nc:error-message", nsMgr)?.InnerText;
            var errorPath = doc.SelectSingleNode("//nc:rpc-error/nc:error-path", nsMgr)?.InnerText;

            var type = errorType switch
            {
                "transport" => ErrorType.Transport,
                "rpc" => ErrorType.Rpc,
                "protocol" => ErrorType.Protocol,
                _ => ErrorType.Application
            };
            var severity = errorSeverity == "warning" ? Severity.Warning : Severity.Error;

            throw new RpcException(type, errorTag, severity, xpath: errorPath, message: errorMessage);
        }

        return doc;
    }

    /// <summary>
    /// Retrieve the running configuration (or specified datastore).
    /// Returns the raw XML data element content.
    /// </summary>
    public async Task<XmlElement?> GetConfigAsync(
        Datastore source = Datastore.Running,
        string? filter = null,
        CancellationToken ct = default)
    {
        var sourceXml = DatastoreToXml(source);
        var filterXml = filter != null
            ? $"<filter type=\"subtree\">{filter}</filter>"
            : "";

        var reply = await SendRpcAsync(
            $"<get-config><source>{sourceXml}</source>{filterXml}</get-config>", ct).ConfigureAwait(false);

        var nsMgr = new XmlNamespaceManager(reply.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        return reply.SelectSingleNode("//nc:data", nsMgr) as XmlElement;
    }

    /// <summary>
    /// Edit the configuration using a pre-serialized XML config fragment.
    /// </summary>
    public async Task EditConfigAsync(
        string configXml,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError,
        CancellationToken ct = default)
    {
        var targetXml = DatastoreToXml(target);
        var defaultOpXml = defaultOp != DefaultOperation.Merge
            ? $"<default-operation>{DefaultOpToString(defaultOp)}</default-operation>"
            : "";
        var errorOptionXml = errorOption != ErrorOption.StopOnError
            ? $"<error-option>{ErrorOptionToString(errorOption)}</error-option>"
            : "";

        await SendRpcAsync(
            $"<edit-config><target>{targetXml}</target>{defaultOpXml}{errorOptionXml}<config>{configXml}</config></edit-config>",
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Edit the configuration using a DotnetYang-generated node.
    /// Serializes the node using WriteXMLAsync with the specified configOnly setting.
    /// </summary>
    public async Task EditConfigAsync(
        IYangXmlSerializable node,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError,
        bool configOnly = true,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings()))
        {
            await node.WriteXMLAsync(writer, configOnly).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        await EditConfigAsync(sb.ToString(), target, defaultOp, errorOption, ct).ConfigureAwait(false);
    }

    /// <summary>Lock a datastore.</summary>
    public async Task LockAsync(Datastore target = Datastore.Running, CancellationToken ct = default)
    {
        var targetXml = DatastoreToXml(target);
        await SendRpcAsync($"<lock><target>{targetXml}</target></lock>", ct).ConfigureAwait(false);
    }

    /// <summary>Unlock a datastore.</summary>
    public async Task UnlockAsync(Datastore target = Datastore.Running, CancellationToken ct = default)
    {
        var targetXml = DatastoreToXml(target);
        await SendRpcAsync($"<unlock><target>{targetXml}</target></unlock>", ct).ConfigureAwait(false);
    }

    /// <summary>Validate the candidate or running configuration.</summary>
    public async Task ValidateAsync(Datastore source = Datastore.Candidate, CancellationToken ct = default)
    {
        var sourceXml = DatastoreToXml(source);
        await SendRpcAsync($"<validate><source>{sourceXml}</source></validate>", ct).ConfigureAwait(false);
    }

    /// <summary>Commit the candidate configuration to running.</summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        await SendRpcAsync("<commit/>", ct).ConfigureAwait(false);
    }

    /// <summary>Discard changes in the candidate datastore.</summary>
    public async Task DiscardChangesAsync(CancellationToken ct = default)
    {
        await SendRpcAsync("<discard-changes/>", ct).ConfigureAwait(false);
    }

    /// <summary>Close the NETCONF session gracefully.</summary>
    public async Task CloseSessionAsync(CancellationToken ct = default)
    {
        try
        {
            await SendRpcAsync("<close-session/>", ct).ConfigureAwait(false);
        }
        catch
        {
            // Best effort
        }
    }

    /// <summary>Kill another NETCONF session.</summary>
    public async Task KillSessionAsync(int sessionId, CancellationToken ct = default)
    {
        await SendRpcAsync($"<kill-session><session-id>{sessionId}</session-id></kill-session>", ct)
            .ConfigureAwait(false);
    }

    /// <summary>Copy one datastore to another.</summary>
    public async Task CopyConfigAsync(
        Datastore source, Datastore target, CancellationToken ct = default)
    {
        var sourceXml = DatastoreToXml(source);
        var targetXml = DatastoreToXml(target);
        await SendRpcAsync(
            $"<copy-config><target>{targetXml}</target><source>{sourceXml}</source></copy-config>", ct)
            .ConfigureAwait(false);
    }

    /// <summary>Delete a datastore (typically startup).</summary>
    public async Task DeleteConfigAsync(Datastore target, CancellationToken ct = default)
    {
        var targetXml = DatastoreToXml(target);
        await SendRpcAsync($"<delete-config><target>{targetXml}</target></delete-config>", ct)
            .ConfigureAwait(false);
    }

    /// <summary>Retrieve operational and configuration data.</summary>
    public async Task<XmlElement?> GetAsync(string? filter = null, CancellationToken ct = default)
    {
        var filterXml = filter != null
            ? $"<filter type=\"subtree\">{filter}</filter>"
            : "";

        var reply = await SendRpcAsync($"<get>{filterXml}</get>", ct).ConfigureAwait(false);

        var nsMgr = new XmlNamespaceManager(reply.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        return reply.SelectSingleNode("//nc:data", nsMgr) as XmlElement;
    }

    /// <summary>
    /// Retrieve configuration and deserialize into a DotnetYang-generated type.
    /// The parseFunc should be the generated ParseAsync method (e.g., YangNode.ParseAsync).
    /// </summary>
    /// <example>
    /// var config = await client.GetConfigAsync(Ietf.Interfaces.YangNode.ParseAsync);
    /// </example>
    public async Task<T> GetConfigAsync<T>(
        Func<XmlReader, Task<T>> parseFunc,
        Datastore source = Datastore.Running,
        string? filter = null,
        CancellationToken ct = default)
    {
        var data = await GetConfigAsync(source, filter, ct).ConfigureAwait(false);
        if (data == null)
            throw new InvalidOperationException("Server returned empty <data/> element");

        var xml = data.InnerXml;
        using var stringReader = new System.IO.StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync().ConfigureAwait(false);
        return await parseFunc(reader).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve operational + config data and deserialize into a DotnetYang-generated type.
    /// </summary>
    public async Task<T> GetAsync<T>(
        Func<XmlReader, Task<T>> parseFunc,
        string? filter = null,
        CancellationToken ct = default)
    {
        var data = await GetAsync(filter, ct).ConfigureAwait(false);
        if (data == null)
            throw new InvalidOperationException("Server returned empty <data/> element");

        var xml = data.InnerXml;
        using var stringReader = new System.IO.StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync().ConfigureAwait(false);
        return await parseFunc(reader).ConfigureAwait(false);
    }

    /// <summary>
    /// Edit configuration with client-side YANG validation (must/when constraints).
    /// Calls YangValidate() on the node before sending to the server.
    /// Throws YangValidationException if constraints are violated.
    /// </summary>
    public async Task EditConfigValidatedAsync(
        IYangXmlSerializable node,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError,
        bool configOnly = true,
        CancellationToken ct = default)
    {
        // Invoke generated YangValidate() via reflection (the method is generated on each node)
        var validateMethod = node.GetType().GetMethod("YangValidate");
        if (validateMethod != null)
        {
            validateMethod.Invoke(node, null);
        }

        await EditConfigAsync(node, target, defaultOp, errorOption, configOnly, ct).ConfigureAwait(false);
    }


    private static string DatastoreToXml(Datastore ds) => ds switch
    {
        Datastore.Running => "<running/>",
        Datastore.Candidate => "<candidate/>",
        Datastore.Startup => "<startup/>",
        _ => "<running/>"
    };

    private static string DefaultOpToString(DefaultOperation op) => op switch
    {
        DefaultOperation.Replace => "replace",
        DefaultOperation.None => "none",
        _ => "merge"
    };

    private static string ErrorOptionToString(ErrorOption opt) => opt switch
    {
        ErrorOption.ContinueOnError => "continue-on-error",
        ErrorOption.RollbackOnError => "rollback-on-error",
        _ => "stop-on-error"
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _input?.Dispose();
        _output?.Dispose();
    }
}
