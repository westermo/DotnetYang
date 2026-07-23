using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Renci.SshNet;
using YangSupport;
using YangSupport.Netconf;

namespace IntegrationTests;

/// <summary>
/// SSH-based NETCONF client wrapper that connects to a NETCONF server
/// and provides the high-level NetconfClient API.
/// Uses SSH.NET's NetConfClient for the SSH subsystem transport,
/// wrapped with DotnetYang's NetconfClient for the typed API.
/// </summary>
public sealed class SshNetconfClient : IDisposable
{
    private readonly NetConfClient _sshClient;
    private readonly NetconfSessionInfo _session = new();

    public NetconfSessionInfo Session => _session;

    private SshNetconfClient(NetConfClient sshClient)
    {
        _sshClient = sshClient;
        ParseCapabilities();
    }

    /// <summary>
    /// Connect to a NETCONF server over SSH and return a high-level client.
    /// </summary>
    public static async Task<SshNetconfClient> ConnectAsync(
        string host, int port, string username, string password,
        CancellationToken ct = default)
    {
        var client = new NetConfClient(host, port, username, password);
        client.OperationTimeout = TimeSpan.FromSeconds(30);
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
        await Task.Run(() => client.Connect(), ct);
        return new SshNetconfClient(client);
    }

    private void ParseCapabilities()
    {
        var caps = _sshClient.ServerCapabilities;
        var nsMgr = new XmlNamespaceManager(caps.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");

        var capNodes = caps.SelectNodes("//nc:capability", nsMgr);
        if (capNodes == null) return;

        foreach (XmlNode cap in capNodes)
        {
            var uri = cap.InnerText?.Trim() ?? "";
            _session.ServerCapabilities.Add(uri);
            if (uri.Contains("base:1.1")) _session.Base11 = true;
            if (uri.Contains("writable-running")) _session.WritableRunning = true;
            if (uri.Contains("candidate")) _session.Candidate = true;
            if (uri.Contains("confirmed-commit")) _session.ConfirmedCommit = true;
            if (uri.Contains("rollback-on-error")) _session.RollbackOnError = true;
            if (uri.Contains("validate")) _session.Validate = true;
            if (uri.Contains("startup")) _session.Startup = true;
            if (uri.Contains("xpath")) _session.Xpath = true;
        }
    }

    private XmlDocument SendRpc(string rpcContent)
    {
        var rpc = $"<rpc xmlns=\"urn:ietf:params:xml:ns:netconf:base:1.0\" message-id=\"0\">{rpcContent}</rpc>";
        return _sshClient.SendReceiveRpc(rpc);
    }

    private void CheckError(XmlDocument reply)
    {
        var nsMgr = new XmlNamespaceManager(reply.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        var errorNode = reply.SelectSingleNode("//nc:rpc-error", nsMgr);
        if (errorNode != null)
        {
            var errorTag = reply.SelectSingleNode("//nc:rpc-error/nc:error-tag", nsMgr)?.InnerText ?? "operation-failed";
            var errorMessage = reply.SelectSingleNode("//nc:rpc-error/nc:error-message", nsMgr)?.InnerText;
            var errorPath = reply.SelectSingleNode("//nc:rpc-error/nc:error-path", nsMgr)?.InnerText;
            throw new RpcException(ErrorType.Application, errorTag, Severity.Error,
                xpath: errorPath, message: errorMessage);
        }
    }

    public async Task<XmlElement?> GetConfigAsync(
        Datastore source = Datastore.Running, string? filter = null)
    {
        var sourceXml = DatastoreToXml(source);
        var filterXml = filter != null ? $"<filter type=\"subtree\">{filter}</filter>" : "";
        var reply = await Task.Run(() => SendRpc(
            $"<get-config><source>{sourceXml}</source>{filterXml}</get-config>"));
        CheckError(reply);
        var nsMgr = new XmlNamespaceManager(reply.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        return reply.SelectSingleNode("//nc:data", nsMgr) as XmlElement;
    }

    public async Task EditConfigAsync(
        string configXml,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError)
    {
        var targetXml = DatastoreToXml(target);
        var defaultOpXml = defaultOp != DefaultOperation.Merge
            ? $"<default-operation>{DefaultOpToStr(defaultOp)}</default-operation>" : "";
        var errorOptionXml = errorOption != ErrorOption.StopOnError
            ? $"<error-option>{ErrorOptToStr(errorOption)}</error-option>" : "";
        var reply = await Task.Run(() => SendRpc(
            $"<edit-config><target>{targetXml}</target>{defaultOpXml}{errorOptionXml}<config>{configXml}</config></edit-config>"));
        CheckError(reply);
    }

    /// <summary>
    /// Edit config using a DotnetYang-generated node.
    /// </summary>
    public async Task EditConfigAsync(
        IYangXmlSerializable node,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError,
        bool configOnly = true)
    {
        var sb = new StringBuilder();
        using (var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings()))
        {
            await node.WriteXMLAsync(writer, configOnly);
            await writer.FlushAsync();
        }
        await EditConfigAsync(sb.ToString(), target, defaultOp, errorOption);
    }

    public async Task LockAsync(Datastore target = Datastore.Running)
    {
        var reply = await Task.Run(() => SendRpc($"<lock><target>{DatastoreToXml(target)}</target></lock>"));
        CheckError(reply);
    }

    public async Task UnlockAsync(Datastore target = Datastore.Running)
    {
        var reply = await Task.Run(() => SendRpc($"<unlock><target>{DatastoreToXml(target)}</target></unlock>"));
        CheckError(reply);
    }

    public async Task ValidateAsync(Datastore source = Datastore.Running)
    {
        var reply = await Task.Run(() => SendRpc($"<validate><source>{DatastoreToXml(source)}</source></validate>"));
        CheckError(reply);
    }

    public async Task CommitAsync()
    {
        var reply = await Task.Run(() => SendRpc("<commit/>"));
        CheckError(reply);
    }

    public async Task DiscardChangesAsync()
    {
        var reply = await Task.Run(() => SendRpc("<discard-changes/>"));
        CheckError(reply);
    }

    public async Task KillSessionAsync(int sessionId)
    {
        var reply = await Task.Run(() => SendRpc($"<kill-session><session-id>{sessionId}</session-id></kill-session>"));
        CheckError(reply);
    }

    public async Task CopyConfigAsync(Datastore source, Datastore target)
    {
        var reply = await Task.Run(() => SendRpc(
            $"<copy-config><target>{DatastoreToXml(target)}</target><source>{DatastoreToXml(source)}</source></copy-config>"));
        CheckError(reply);
    }

    public async Task DeleteConfigAsync(Datastore target)
    {
        var reply = await Task.Run(() => SendRpc($"<delete-config><target>{DatastoreToXml(target)}</target></delete-config>"));
        CheckError(reply);
    }

    /// <summary>
    /// Get config and deserialize into a DotnetYang-generated type.
    /// </summary>
    public async Task<T> GetConfigAsync<T>(
        Func<XmlReader, Task<T>> parseFunc,
        Datastore source = Datastore.Running,
        string? filter = null)
    {
        var data = await GetConfigAsync(source, filter);
        if (data == null)
            throw new InvalidOperationException("Server returned empty <data/> element");

        var xml = data.InnerXml;
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync();
        return await parseFunc(reader);
    }

    /// <summary>
    /// Edit config with client-side YANG validation (must/when/cardinality).
    /// </summary>
    public async Task EditConfigValidatedAsync(
        IYangXmlSerializable node,
        Datastore target = Datastore.Running,
        DefaultOperation defaultOp = DefaultOperation.Merge,
        ErrorOption errorOption = ErrorOption.StopOnError,
        bool configOnly = true)
    {
        var validateMethod = node.GetType().GetMethod("YangValidate");
        validateMethod?.Invoke(node, null);

        await EditConfigAsync(node, target, defaultOp, errorOption, configOnly);
    }

    public void CloseSession()
    {
        try { _sshClient.SendCloseRpc(); } catch { }
    }

    public void Dispose()
    {
        try { CloseSession(); } catch { }
        if (_sshClient.IsConnected) _sshClient.Disconnect();
        _sshClient.Dispose();
    }

    private static string DatastoreToXml(Datastore ds) => ds switch
    {
        Datastore.Running => "<running/>",
        Datastore.Candidate => "<candidate/>",
        Datastore.Startup => "<startup/>",
        _ => "<running/>"
    };

    private static string DefaultOpToStr(DefaultOperation op) => op switch
    {
        DefaultOperation.Replace => "replace",
        DefaultOperation.None => "none",
        _ => "merge"
    };

    private static string ErrorOptToStr(ErrorOption opt) => opt switch
    {
        ErrorOption.ContinueOnError => "continue-on-error",
        ErrorOption.RollbackOnError => "rollback-on-error",
        _ => "stop-on-error"
    };
}
