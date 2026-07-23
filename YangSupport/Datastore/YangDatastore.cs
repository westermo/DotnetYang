using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace YangSupport.Datastore;

/// <summary>
/// Represents a NETCONF datastore (running, candidate, or startup).
/// Holds a typed configuration root and provides serialization/deserialization.
/// 
/// The type parameter T is the generated Configuration class.
/// </summary>
public class YangDatastore<T> where T : class, IYangNode, IYangXmlSerializable, new()
{
    private T _data;
    private readonly ReaderWriterLockSlim _lock = new();
    private int? _lockedBySession;

    public YangDatastore()
    {
        _data = new T();
    }

    public YangDatastore(T initial)
    {
        _data = initial;
    }

    /// <summary>Current configuration data (read-only snapshot).</summary>
    public T Data
    {
        get
        {
            _lock.EnterReadLock();
            try { return _data; }
            finally { _lock.ExitReadLock(); }
        }
    }

    /// <summary>
    /// Replace the entire datastore content.
    /// </summary>
    public void Replace(T newData)
    {
        _lock.EnterWriteLock();
        try { _data = newData; }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Apply a modification function to the datastore atomically.
    /// </summary>
    public void Modify(Action<T> modifier)
    {
        _lock.EnterWriteLock();
        try { modifier(_data); }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Serialize the datastore content to XML.
    /// </summary>
    public async Task<string> SerializeAsync(bool configOnly = false)
    {
        _lock.EnterReadLock();
        try
        {
            var sb = new StringBuilder();
            using var writer = XmlWriter.Create(sb, SerializationHelper.GetStandardWriterSettings());
            await _data.WriteXMLAsync(writer, configOnly).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            return sb.ToString();
        }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>
    /// Deserialize XML into the datastore, replacing current content.
    /// </summary>
    public async Task LoadAsync(string xml, Func<XmlReader, Task<T>> parseFunc)
    {
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, SerializationHelper.GetStandardReaderSettings());
        await reader.ReadAsync().ConfigureAwait(false);
        var parsed = await parseFunc(reader).ConfigureAwait(false);
        Replace(parsed);
    }

    /// <summary>
    /// Acquire a session lock on this datastore (RFC 6241 §7.5).
    /// </summary>
    /// <returns>True if lock acquired, false if already locked by another session.</returns>
    public bool TryLock(int sessionId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_lockedBySession.HasValue && _lockedBySession.Value != sessionId)
                return false;
            _lockedBySession = sessionId;
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>
    /// Release the session lock.
    /// </summary>
    public bool Unlock(int sessionId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_lockedBySession != sessionId)
                return false;
            _lockedBySession = null;
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Whether the datastore is currently locked.</summary>
    public bool IsLocked => _lockedBySession.HasValue;

    /// <summary>Session ID holding the lock, or null.</summary>
    public int? LockedBy => _lockedBySession;
}
