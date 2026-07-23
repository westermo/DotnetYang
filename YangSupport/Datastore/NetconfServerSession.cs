using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Datastore;

/// <summary>
/// NETCONF server session handler. Processes incoming NETCONF RPCs against
/// the datastore manager and delegates module-specific operations to IYangServer.
///
/// Usage:
///   var manager = new DatastoreManager&lt;Configuration&gt;(Configuration.ParseAsync);
///   var session = new NetconfServerSession&lt;Configuration&gt;(manager, sessionId);
///   await session.HandleRpcAsync(inputStream, outputStream);
/// </summary>
public class NetconfServerSession<T> where T : class, IYangNode, IYangXmlSerializable, new()
{
    private readonly DatastoreManager<T> _datastores;
    private readonly int _sessionId;
    private static int _nextSessionId;

    public int SessionId => _sessionId;

    public NetconfServerSession(DatastoreManager<T> datastores, int? sessionId = null)
    {
        _datastores = datastores;
        _sessionId = sessionId ?? Interlocked.Increment(ref _nextSessionId);
    }

    /// <summary>
    /// Generate the server hello message with capabilities.
    /// </summary>
    public string GetHello()
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <hello xmlns="urn:ietf:params:xml:ns:netconf:base:1.0">
              <capabilities>
                <capability>urn:ietf:params:netconf:base:1.0</capability>
                <capability>urn:ietf:params:netconf:base:1.1</capability>
                <capability>urn:ietf:params:netconf:capability:writable-running:1.0</capability>
                <capability>urn:ietf:params:netconf:capability:candidate:1.0</capability>
                <capability>urn:ietf:params:netconf:capability:confirmed-commit:1.1</capability>
                <capability>urn:ietf:params:netconf:capability:rollback-on-error:1.0</capability>
                <capability>urn:ietf:params:netconf:capability:validate:1.1</capability>
                <capability>urn:ietf:params:netconf:capability:startup:1.0</capability>
                <capability>urn:ietf:params:netconf:capability:notification:1.0</capability>
              </capabilities>
              <session-id>{_sessionId}</session-id>
            </hello>
            """;
    }

    /// <summary>
    /// Handle a single NETCONF RPC from the input stream and write the reply to output.
    /// Returns false if the session should be closed (close-session received).
    /// </summary>
    public async Task<bool> HandleRpcAsync(Stream input, Stream output)
    {
        using var reader = XmlReader.Create(input, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync().ConfigureAwait(false);

        if (reader.Name != "rpc" ||
            reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0")
        {
            await output.SerializeRegularExceptionAsync(
                new Exception($"Expected <rpc> but got <{reader.Name}>"), null).ConfigureAwait(false);
            return true;
        }

        var messageId = reader["message-id"];
        await reader.ReadAsync().ConfigureAwait(false);

        try
        {
            return await DispatchRpcAsync(reader, output, messageId).ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            await ex.SerializeAsync(output, messageId).ConfigureAwait(false);
            return true;
        }
        catch (YangValidationException ex)
        {
            var rpcEx = new RpcException(ErrorType.Application, "invalid-value", Severity.Error,
                message: ex.Message, xpath: ex.SchemaPath);
            await rpcEx.SerializeAsync(output, messageId).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await output.SerializeRegularExceptionAsync(ex, messageId).ConfigureAwait(false);
            return true;
        }
    }

    private async Task<bool> DispatchRpcAsync(XmlReader reader, Stream output, string? messageId)
    {
        switch (reader.Name)
        {
            case "get-config":
                await HandleGetConfigAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "edit-config":
                await HandleEditConfigAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "copy-config":
                await HandleCopyConfigAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "delete-config":
                await HandleDeleteConfigAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "lock":
                await HandleLockAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "unlock":
                await HandleUnlockAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "validate":
                await HandleValidateAsync(reader, output, messageId).ConfigureAwait(false);
                return true;

            case "commit":
                await HandleCommitAsync(output, messageId).ConfigureAwait(false);
                return true;

            case "discard-changes":
                await HandleDiscardChangesAsync(output, messageId).ConfigureAwait(false);
                return true;

            case "close-session":
                await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
                return false; // Signal to close session

            case "kill-session":
                await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
                return true;

            default:
                throw new RpcException(ErrorType.Protocol, "unknown-element", Severity.Error,
                    message: $"Unknown RPC operation: {reader.Name}");
        }
    }

    private async Task HandleGetConfigAsync(XmlReader reader, Stream output, string? messageId)
    {
        var datastore = Netconf.Datastore.Running;

        // Parse source element
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "source":
                        await reader.ReadAsync().ConfigureAwait(false);
                        datastore = ParseDatastoreElement(reader);
                        break;
                }
            }
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "get-config")
                break;
        }

        var ds = _datastores.GetDatastore(datastore);
        var xml = await ds.SerializeAsync(configOnly: true).ConfigureAwait(false);

        using var writer = XmlWriter.Create(output, SerializationHelper.GetStandardWriterSettings());
        await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0")
            .ConfigureAwait(false);
        if (messageId != null)
            await writer.WriteAttributeStringAsync(null, "message-id", null, messageId).ConfigureAwait(false);
        await writer.WriteStartElementAsync(null, "data", "urn:ietf:params:xml:ns:netconf:base:1.0")
            .ConfigureAwait(false);
        await writer.WriteRawAsync(xml).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false); // data
        await writer.WriteEndElementAsync().ConfigureAwait(false); // rpc-reply
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private async Task HandleEditConfigAsync(XmlReader reader, Stream output, string? messageId)
    {
        var target = Netconf.Datastore.Running;

        // Navigate to config content
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "target":
                        await reader.ReadAsync().ConfigureAwait(false);
                        target = ParseDatastoreElement(reader);
                        break;
                    case "config":
                        // Read the config content and merge into target datastore
                        var configContent = await reader.ReadInnerXmlAsync().ConfigureAwait(false);
                        var ds = _datastores.GetDatastore(target);
                        if (ds.IsLocked && ds.LockedBy != _sessionId)
                            throw new RpcException(ErrorType.Protocol, "in-use", Severity.Error,
                                message: $"Datastore is locked by session {ds.LockedBy}");

                        await ds.LoadAsync(configContent, _datastores.GetParseFunc())
                            .ConfigureAwait(false);
                        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
                        return;
                }
            }
        }

        throw new RpcException(ErrorType.Protocol, "missing-element", Severity.Error,
            message: "Missing <config> element in edit-config");
    }

    private async Task HandleCopyConfigAsync(XmlReader reader, Stream output, string? messageId)
    {
        var source = Netconf.Datastore.Running;
        var target = Netconf.Datastore.Startup;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "source":
                        await reader.ReadAsync().ConfigureAwait(false);
                        source = ParseDatastoreElement(reader);
                        break;
                    case "target":
                        await reader.ReadAsync().ConfigureAwait(false);
                        target = ParseDatastoreElement(reader);
                        break;
                }
            }
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "copy-config")
                break;
        }

        await _datastores.CopyConfigAsync(source, target).ConfigureAwait(false);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleDeleteConfigAsync(XmlReader reader, Stream output, string? messageId)
    {
        var target = Netconf.Datastore.Startup;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "target")
            {
                await reader.ReadAsync().ConfigureAwait(false);
                target = ParseDatastoreElement(reader);
                break;
            }
        }

        _datastores.DeleteConfig(target);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleLockAsync(XmlReader reader, Stream output, string? messageId)
    {
        var target = Netconf.Datastore.Running;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "target")
            {
                await reader.ReadAsync().ConfigureAwait(false);
                target = ParseDatastoreElement(reader);
                break;
            }
        }

        _datastores.Lock(target, _sessionId);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleUnlockAsync(XmlReader reader, Stream output, string? messageId)
    {
        var target = Netconf.Datastore.Running;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "target")
            {
                await reader.ReadAsync().ConfigureAwait(false);
                target = ParseDatastoreElement(reader);
                break;
            }
        }

        _datastores.Unlock(target, _sessionId);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleValidateAsync(XmlReader reader, Stream output, string? messageId)
    {
        var source = Netconf.Datastore.Candidate;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "source")
            {
                await reader.ReadAsync().ConfigureAwait(false);
                source = ParseDatastoreElement(reader);
                break;
            }
        }

        _datastores.Validate(source);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleCommitAsync(Stream output, string? messageId)
    {
        await _datastores.CommitAsync().ConfigureAwait(false);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private async Task HandleDiscardChangesAsync(Stream output, string? messageId)
    {
        await _datastores.DiscardChangesAsync().ConfigureAwait(false);
        await WriteOkReplyAsync(output, messageId).ConfigureAwait(false);
    }

    private static Netconf.Datastore ParseDatastoreElement(XmlReader reader)
    {
        // Reader should be on the element inside <source> or <target>: <running/>, <candidate/>, etc.
        while (reader.NodeType != XmlNodeType.Element)
        {
            if (!reader.Read()) break;
        }

        return reader.Name switch
        {
            "running" => Netconf.Datastore.Running,
            "candidate" => Netconf.Datastore.Candidate,
            "startup" => Netconf.Datastore.Startup,
            _ => Netconf.Datastore.Running
        };
    }

    private static async Task WriteOkReplyAsync(Stream output, string? messageId)
    {
        using var writer = XmlWriter.Create(output, SerializationHelper.GetStandardWriterSettings());
        await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0")
            .ConfigureAwait(false);
        if (messageId != null)
            await writer.WriteAttributeStringAsync(null, "message-id", null, messageId).ConfigureAwait(false);
        await writer.WriteStartElementAsync(null, "ok", "urn:ietf:params:xml:ns:netconf:base:1.0")
            .ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }
}
