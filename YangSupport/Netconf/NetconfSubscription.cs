using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Netconf;

/// <summary>
/// NETCONF notification subscription client (RFC 5277).
/// After calling CreateSubscription, notifications are received asynchronously
/// and can be consumed via the typed ReadNotificationsAsync method.
/// 
/// Usage:
///   var sub = await NetconfSubscription.CreateAsync(inputStream, outputStream);
///   await sub.CreateSubscriptionAsync();
///   await foreach (var (eventTime, xml) in sub.ReadNotificationsAsync())
///   {
///       // process notification
///   }
/// </summary>
public class NetconfSubscription : IDisposable
{
    private readonly Stream _input;
    private readonly Stream _output;
    private readonly bool _base11;
    private readonly Channel<(DateTime EventTime, string Xml)> _notifications;
    private readonly CancellationTokenSource _cts = new();
    private Task? _readLoop;
    private int _messageId;
    private TaskCompletionSource<XmlDocument>? _pendingReply;

    private NetconfSubscription(Stream input, Stream output, bool base11)
    {
        _input = input;
        _output = output;
        _base11 = base11;
        _notifications = Channel.CreateUnbounded<(DateTime, string)>();
    }

    /// <summary>
    /// Connect and perform hello exchange, then return a subscription-ready client.
    /// </summary>
    public static async Task<NetconfSubscription> CreateAsync(
        Stream input, Stream output, CancellationToken ct = default)
    {
        // Read server hello (EOM framed)
        var serverHello = await NetconfFraming.ReadEomMessageAsync(input, ct).ConfigureAwait(false);
        bool base11 = serverHello.Contains("base:1.1");

        // Send client hello
        var clientHello = """
            <?xml version="1.0" encoding="UTF-8"?>
            <hello xmlns="urn:ietf:params:xml:ns:netconf:base:1.0">
              <capabilities>
                <capability>urn:ietf:params:netconf:base:1.0</capability>
                <capability>urn:ietf:params:netconf:base:1.1</capability>
                <capability>urn:ietf:params:netconf:capability:notification:1.0</capability>
              </capabilities>
            </hello>
            """;
        var encoded = NetconfFraming.EncodeEom(clientHello);
        await output.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
        await output.FlushAsync().ConfigureAwait(false);

        return new NetconfSubscription(input, output, base11);
    }

    /// <summary>
    /// Send create-subscription RPC (RFC 5277).
    /// After this, the read loop starts and notifications are dispatched.
    /// </summary>
    public async Task CreateSubscriptionAsync(
        string? stream = null, string? filter = null,
        string? startTime = null, string? stopTime = null,
        CancellationToken ct = default)
    {
        var streamXml = stream != null ? $"<stream>{stream}</stream>" : "";
        var filterXml = filter != null ? $"<filter type=\"subtree\">{filter}</filter>" : "";
        var startXml = startTime != null ? $"<startTime>{startTime}</startTime>" : "";
        var stopXml = stopTime != null ? $"<stopTime>{stopTime}</stopTime>" : "";

        var msgId = Interlocked.Increment(ref _messageId);
        var rpc = $"""
            <rpc xmlns="urn:ietf:params:xml:ns:netconf:base:1.0" message-id="{msgId}">
              <create-subscription xmlns="urn:ietf:params:xml:ns:netconf:notification:1.0">
                {streamXml}{filterXml}{startXml}{stopXml}
              </create-subscription>
            </rpc>
            """;

        // Send the RPC
        byte[] data;
        if (_base11)
            data = NetconfFraming.EncodeChunked(rpc);
        else
            data = NetconfFraming.EncodeEom(rpc);

        // Set up reply expectation before starting read loop
        _pendingReply = new TaskCompletionSource<XmlDocument>();

        await _output.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        await _output.FlushAsync().ConfigureAwait(false);

        // Start the read loop
        _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));

        // Wait for the RPC reply (ok or error)
        var reply = await _pendingReply.Task.ConfigureAwait(false);
        _pendingReply = null;

        // Check for error
        var nsMgr = new XmlNamespaceManager(reply.NameTable);
        nsMgr.AddNamespace("nc", "urn:ietf:params:xml:ns:netconf:base:1.0");
        var errorNode = reply.SelectSingleNode("//nc:rpc-error", nsMgr);
        if (errorNode != null)
        {
            var errorTag = reply.SelectSingleNode("//nc:rpc-error/nc:error-tag", nsMgr)?.InnerText ?? "operation-failed";
            var errorMessage = reply.SelectSingleNode("//nc:rpc-error/nc:error-message", nsMgr)?.InnerText;
            throw new RpcException(ErrorType.Application, errorTag, Severity.Error, message: errorMessage);
        }
    }

    /// <summary>
    /// Read notifications as an async stream. Each notification includes the event time
    /// and the raw inner XML of the notification element.
    /// </summary>
    public async IAsyncEnumerable<(DateTime EventTime, string Xml)> ReadNotificationsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            (DateTime, string) item;
            try
            {
                item = await _notifications.Reader.ReadAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (ChannelClosedException)
            {
                yield break;
            }
            yield return item;
        }
    }

    /// <summary>
    /// Read notifications and parse them into typed objects using the provided parse function.
    /// The parse function receives an XmlReader positioned at the notification content element.
    /// </summary>
    public async IAsyncEnumerable<(DateTime EventTime, T Notification)> ReadNotificationsAsync<T>(
        Func<XmlReader, Task<T>> parseFunc,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (eventTime, xml) in ReadNotificationsAsync(ct).ConfigureAwait(false))
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
            await reader.ReadAsync().ConfigureAwait(false);
            var notification = await parseFunc(reader).ConfigureAwait(false);
            yield return (eventTime, notification);
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                string message;
                if (_base11)
                    message = await NetconfFraming.ReadChunkedMessageAsync(_input, ct).ConfigureAwait(false);
                else
                    message = await NetconfFraming.ReadEomMessageAsync(_input, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(message)) continue;

                var doc = new XmlDocument();
                doc.LoadXml(message);

                var root = doc.DocumentElement;
                if (root == null) continue;

                if (root.LocalName == "rpc-reply")
                {
                    // Dispatch to pending RPC reply
                    _pendingReply?.TrySetResult(doc);
                }
                else if (root.LocalName == "notification")
                {
                    // Parse RFC 5277 notification: <notification><eventTime>...</eventTime><content/></notification>
                    var nsMgr = new XmlNamespaceManager(doc.NameTable);
                    nsMgr.AddNamespace("ncn", "urn:ietf:params:xml:ns:netconf:notification:1.0");

                    var eventTimeNode = root.SelectSingleNode("ncn:eventTime", nsMgr);
                    var eventTime = DateTime.UtcNow;
                    if (eventTimeNode != null && DateTime.TryParse(eventTimeNode.InnerText, out var parsed))
                        eventTime = parsed;

                    // The notification content is the element after eventTime
                    string contentXml = "";
                    foreach (XmlNode child in root.ChildNodes)
                    {
                        if (child is XmlElement el && el.LocalName != "eventTime")
                        {
                            contentXml = el.OuterXml;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(contentXml))
                    {
                        _notifications.Writer.TryWrite((eventTime, contentXml));
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            // Stream closed or read error — close the notification channel
        }
        finally
        {
            _notifications.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _readLoop?.Wait(TimeSpan.FromSeconds(2));
        _cts.Dispose();
        _input?.Dispose();
        _output?.Dispose();
    }
}
