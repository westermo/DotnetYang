using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Datastore;

/// <summary>
/// Manages the three standard NETCONF datastores (running, candidate, startup)
/// and provides operations for copying between them.
///
/// Usage:
///   var manager = new DatastoreManager&lt;Configuration&gt;(Configuration.ParseAsync);
///   manager.Running.Modify(cfg => cfg.Interfaces = ...);
///   await manager.CopyConfigAsync(Netconf.Datastore.Running, Netconf.Datastore.Startup);
///   await manager.CommitAsync(); // candidate → running
/// </summary>
public class DatastoreManager<T> where T : class, IYangNode, IYangXmlSerializable, new()
{
    private readonly Func<XmlReader, Task<T>> _parseFunc;

    /// <summary>The running (active) configuration datastore.</summary>
    public YangDatastore<T> Running { get; }

    /// <summary>Get the parse function used to deserialize configurations.</summary>
    public Func<XmlReader, Task<T>> GetParseFunc() => _parseFunc;

    /// <summary>The candidate configuration datastore (staging area).</summary>
    public YangDatastore<T> Candidate { get; }

    /// <summary>The startup configuration datastore (persisted across reboots).</summary>
    public YangDatastore<T> Startup { get; }

    /// <summary>
    /// Create a new datastore manager with empty datastores.
    /// </summary>
    /// <param name="parseFunc">The generated ParseAsync method for the Configuration type.</param>
    public DatastoreManager(Func<XmlReader, Task<T>> parseFunc)
    {
        _parseFunc = parseFunc;
        Running = new YangDatastore<T>();
        Candidate = new YangDatastore<T>();
        Startup = new YangDatastore<T>();
    }

    /// <summary>
    /// Get the datastore for the given target.
    /// </summary>
    public YangDatastore<T> GetDatastore(Netconf.Datastore datastore) => datastore switch
    {
        Netconf.Datastore.Running => Running,
        Netconf.Datastore.Candidate => Candidate,
        Netconf.Datastore.Startup => Startup,
        _ => throw new ArgumentException($"Unknown datastore: {datastore}")
    };

    /// <summary>
    /// Copy one datastore to another (RFC 6241 §7.3).
    /// Serializes the source and deserializes into the target.
    /// </summary>
    public async Task CopyConfigAsync(Netconf.Datastore source, Netconf.Datastore target)
    {
        var sourceDs = GetDatastore(source);
        var targetDs = GetDatastore(target);

        if (targetDs.IsLocked)
            throw new RpcException(ErrorType.Protocol, "in-use", Severity.Error,
                message: $"Target datastore '{target}' is locked");

        var xml = await sourceDs.SerializeAsync(configOnly: true).ConfigureAwait(false);
        await targetDs.LoadAsync(xml, _parseFunc).ConfigureAwait(false);
    }

    /// <summary>
    /// Commit candidate to running (RFC 6241 §8.4).
    /// </summary>
    public async Task CommitAsync()
    {
        if (Running.IsLocked)
            throw new RpcException(ErrorType.Protocol, "in-use", Severity.Error,
                message: "Running datastore is locked");

        var xml = await Candidate.SerializeAsync(configOnly: true).ConfigureAwait(false);
        await Running.LoadAsync(xml, _parseFunc).ConfigureAwait(false);
    }

    /// <summary>
    /// Discard candidate changes — reset candidate to match running.
    /// </summary>
    public async Task DiscardChangesAsync()
    {
        var xml = await Running.SerializeAsync(configOnly: true).ConfigureAwait(false);
        await Candidate.LoadAsync(xml, _parseFunc).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate the specified datastore by calling YangValidate() on its content.
    /// Throws YangValidationException if constraints are violated.
    /// </summary>
    public void Validate(Netconf.Datastore datastore)
    {
        var ds = GetDatastore(datastore);
        var data = ds.Data;
        var validateMethod = data.GetType().GetMethod("YangValidate");
        if (validateMethod != null)
        {
            validateMethod.Invoke(data, null);
        }
    }

    /// <summary>
    /// Lock a datastore for a session.
    /// </summary>
    public void Lock(Netconf.Datastore datastore, int sessionId)
    {
        var ds = GetDatastore(datastore);
        if (!ds.TryLock(sessionId))
        {
            throw new RpcException(ErrorType.Protocol, "lock-denied", Severity.Error,
                message: $"Datastore '{datastore}' is already locked by session {ds.LockedBy}",
                info: new() { ["session-id"] = ds.LockedBy?.ToString() ?? "" });
        }
    }

    /// <summary>
    /// Unlock a datastore for a session.
    /// </summary>
    public void Unlock(Netconf.Datastore datastore, int sessionId)
    {
        var ds = GetDatastore(datastore);
        if (!ds.Unlock(sessionId))
        {
            throw new RpcException(ErrorType.Protocol, "operation-failed", Severity.Error,
                message: $"Datastore '{datastore}' is not locked by session {sessionId}");
        }
    }

    /// <summary>
    /// Delete a datastore (typically startup). Resets it to empty.
    /// Cannot delete running or candidate.
    /// </summary>
    public void DeleteConfig(Netconf.Datastore datastore)
    {
        if (datastore == Netconf.Datastore.Running)
            throw new RpcException(ErrorType.Protocol, "operation-not-supported", Severity.Error,
                message: "Cannot delete the running datastore");

        var ds = GetDatastore(datastore);
        if (ds.IsLocked)
            throw new RpcException(ErrorType.Protocol, "in-use", Severity.Error,
                message: $"Datastore '{datastore}' is locked");

        ds.Replace(new T());
    }

    /// <summary>
    /// Persist running to a file (for startup persistence).
    /// </summary>
    public async Task SaveToFileAsync(string filePath)
    {
        var xml = await Running.SerializeAsync(configOnly: true).ConfigureAwait(false);
        File.WriteAllText(filePath, xml);
    }

    /// <summary>
    /// Load startup from a file and optionally copy to running.
    /// </summary>
    public async Task LoadFromFileAsync(string filePath, bool copyToRunning = true)
    {
        if (!File.Exists(filePath)) return;

        var xml = File.ReadAllText(filePath);
        await Startup.LoadAsync(xml, _parseFunc).ConfigureAwait(false);

        if (copyToRunning)
        {
            await Running.LoadAsync(xml, _parseFunc).ConfigureAwait(false);
            await Candidate.LoadAsync(xml, _parseFunc).ConfigureAwait(false);
        }
    }
}
