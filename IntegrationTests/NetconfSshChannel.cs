using System.Text;
using System.Xml;
using Renci.SshNet;
using YangSupport;

namespace IntegrationTests;

/// <summary>
/// NETCONF channel using SSH.NET's built-in NetConfClient.
/// Handles the NETCONF subsystem, hello exchange, and EOM framing automatically.
/// Implements IChannel so DotnetYang-generated code can send/receive NETCONF messages.
/// </summary>
public sealed class NetconfSshChannel : IChannel, IAsyncDisposable
{
    private readonly NetConfClient _client;
    private readonly MemoryStream _writeBuffer = new();
    private readonly MemoryStream _readBuffer = new();

    public Stream WriteStream => _writeBuffer;
    public Stream ReadStream => _readBuffer;

    public string Host { get; }
    public int Port { get; }

    private NetconfSshChannel(NetConfClient client, string host, int port)
    {
        _client = client;
        Host = host;
        Port = port;
    }

    /// <summary>
    /// Connect to a NETCONF server using SSH.NET's NetConfClient.
    /// This properly uses the NETCONF SSH subsystem (RFC 6242).
    /// </summary>
    public static async Task<NetconfSshChannel> ConnectAsync(
        string host, int port, string username, string password,
        CancellationToken ct = default)
    {
        var client = new NetConfClient(host, port, username, password);
        client.OperationTimeout = TimeSpan.FromSeconds(30);
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);

        await Task.Run(() => client.Connect(), ct);

        return new NetconfSshChannel(client, host, port);
    }

    public async Task Send()
    {
        // Read what was written to the write buffer (the RPC XML)
        var bytes = _writeBuffer.ToArray();
        _writeBuffer.SetLength(0);
        _writeBuffer.Position = 0;

        // Decode, trimming any BOM or leading whitespace that XmlDocument.LoadXml can't handle
        var message = new UTF8Encoding(false).GetString(bytes).TrimStart('\uFEFF').Trim();

        // Send via NetConfClient and get the reply
        var reply = await Task.Run(() => _client.SendReceiveRpc(message));

        // Write the reply XML into the read buffer
        _readBuffer.SetLength(0);
        var responseBytes = Encoding.UTF8.GetBytes(reply.OuterXml);
        await _readBuffer.WriteAsync(responseBytes);
        _readBuffer.Position = 0;
    }

    /// <summary>
    /// Get the server capabilities reported during the hello exchange.
    /// </summary>
    public XmlDocument ServerCapabilities => _client.ServerCapabilities;

    public void Dispose()
    {
        _writeBuffer.Dispose();
        _readBuffer.Dispose();
        if (_client.IsConnected)
        {
            try { _client.SendCloseRpc(); } catch { /* best effort */ }
            _client.Disconnect();
        }
        _client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _writeBuffer.DisposeAsync();
        await _readBuffer.DisposeAsync();
        if (_client.IsConnected)
        {
            try { await Task.Run(() => _client.SendCloseRpc()); } catch { /* best effort */ }
            _client.Disconnect();
        }
        _client.Dispose();
    }
}

