using System.Collections;

namespace YangSupport;

/// <summary>
/// A keyed, ordered list that provides both dictionary-style key lookup and list-style ordered enumeration.
/// Used for YANG list nodes that have a "key" statement.
/// </summary>
/// <typeparam name="TKey">The key type (single key) or a ValueTuple of key types (composite key).</typeparam>
/// <typeparam name="TValue">The list entry type.</typeparam>
public class YangList<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>, ICollection where TKey : notnull
{
    private readonly List<TValue> _list = new();
    private readonly Dictionary<TKey, TValue> _dict = new();
    private readonly Func<TValue, TKey> _keyExtractor;

    // Non-generic ICollection support: the translator emits casts to
    // System.Collections.ICollection to read Count uniformly across YangList,
    // List<T>, and arrays.
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);

    /// <summary>
    /// Invoked after an entry has been added to this list. Used by generated code
    /// to wire the entry's <c>Parent</c> property to the containing object.
    /// </summary>
    public Action<TValue>? OnAdded { get; set; }

    /// <summary>
    /// Invoked after an entry has been removed from this list.
    /// </summary>
    public Action<TValue>? OnRemoved { get; set; }

    /// <summary>
    /// Creates a new YangList with the specified key extractor function.
    /// </summary>
    /// <param name="keyExtractor">A function that extracts the key from a list entry.</param>
    public YangList(Func<TValue, TKey> keyExtractor)
    {
        _keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));
    }

    /// <summary>
    /// Gets or sets an entry by its key.
    /// </summary>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            if (_dict.TryGetValue(key, out var existing))
            {
                var idx = _list.IndexOf(existing);
                _list[idx] = value;
                _dict[key] = value;
                OnRemoved?.Invoke(existing);
                OnAdded?.Invoke(value);
            }
            else
            {
                _list.Add(value);
                _dict[key] = value;
                OnAdded?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets an entry by index (preserves insertion order).
    /// </summary>
    public TValue this[int index]
    {
        get => _list[index];
        set
        {
            var oldItem = _list[index];
            var oldKey = _keyExtractor(oldItem);
            _dict.Remove(oldKey);
            _list[index] = value;
            var newKey = _keyExtractor(value);
            _dict[newKey] = value;
            OnRemoved?.Invoke(oldItem);
            OnAdded?.Invoke(value);
        }
    }

    /// <summary>
    /// Tries to get an entry by its key.
    /// </summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dict.TryGetValue(key, out value!);
    }

    /// <summary>
    /// Checks if an entry with the specified key exists.
    /// </summary>
    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    /// <summary>
    /// Gets all keys in insertion order.
    /// </summary>
    public IEnumerable<TKey> Keys => _list.Select(_keyExtractor);

    public int Count => _list.Count;
    public bool IsReadOnly => false;

    public void Add(TValue item)
    {
        var key = _keyExtractor(item);
        if (_dict.ContainsKey(key))
        {
            throw new ArgumentException($"An entry with key '{key}' already exists in the list.");
        }
        _list.Add(item);
        _dict[key] = item;
        OnAdded?.Invoke(item);
    }

    /// <summary>
    /// Adds or updates an entry. If an entry with the same key exists, it is replaced.
    /// </summary>
    public void AddOrReplace(TValue item)
    {
        var key = _keyExtractor(item);
        if (_dict.TryGetValue(key, out var existing))
        {
            var idx = _list.IndexOf(existing);
            _list[idx] = item;
            _dict[key] = item;
            OnRemoved?.Invoke(existing);
            OnAdded?.Invoke(item);
        }
        else
        {
            _list.Add(item);
            _dict[key] = item;
            OnAdded?.Invoke(item);
        }
    }

    public void Clear()
    {
        if (OnRemoved is { } cb)
        {
            foreach (var item in _list) cb(item);
        }
        _list.Clear();
        _dict.Clear();
    }

    public bool Contains(TValue item) => _list.Contains(item);

    public void CopyTo(TValue[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public IEnumerator<TValue> GetEnumerator() => _list.GetEnumerator();

    public int IndexOf(TValue item) => _list.IndexOf(item);

    public void Insert(int index, TValue item)
    {
        var key = _keyExtractor(item);
        if (_dict.ContainsKey(key))
        {
            throw new ArgumentException($"An entry with key '{key}' already exists in the list.");
        }
        _list.Insert(index, item);
        _dict[key] = item;
        OnAdded?.Invoke(item);
    }

    public bool Remove(TValue item)
    {
        var key = _keyExtractor(item);
        if (_list.Remove(item))
        {
            _dict.Remove(key);
            OnRemoved?.Invoke(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes an entry by its key.
    /// </summary>
    public bool RemoveByKey(TKey key)
    {
        if (_dict.TryGetValue(key, out var item))
        {
            _dict.Remove(key);
            _list.Remove(item);
            OnRemoved?.Invoke(item);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        var item = _list[index];
        var key = _keyExtractor(item);
        _dict.Remove(key);
        _list.RemoveAt(index);
        OnRemoved?.Invoke(item);
    }

    /// <summary>
    /// Inserts an item before the entry with the specified reference key.
    /// Per RFC 7950 §7.7.7: ordered-by user lists support insert="before" operations.
    /// </summary>
    public void InsertBefore(TKey referenceKey, TValue item)
    {
        if (!_dict.TryGetValue(referenceKey, out var refItem))
            throw new ArgumentException($"Reference key '{referenceKey}' not found in the list.");
        var idx = _list.IndexOf(refItem);
        Insert(idx, item);
    }

    /// <summary>
    /// Inserts an item after the entry with the specified reference key.
    /// Per RFC 7950 §7.7.7: ordered-by user lists support insert="after" operations.
    /// </summary>
    public void InsertAfter(TKey referenceKey, TValue item)
    {
        if (!_dict.TryGetValue(referenceKey, out var refItem))
            throw new ArgumentException($"Reference key '{referenceKey}' not found in the list.");
        var idx = _list.IndexOf(refItem);
        Insert(idx + 1, item);
    }

    /// <summary>
    /// Moves an existing entry to a new position relative to a reference entry.
    /// Per RFC 7950 §7.7.7: ordered-by user lists support move operations in edit-config.
    /// </summary>
    /// <param name="entryKey">Key of the entry to move.</param>
    /// <param name="referenceKey">Key of the reference entry.</param>
    /// <param name="before">If true, move before reference; if false, move after.</param>
    public void Move(TKey entryKey, TKey referenceKey, bool before)
    {
        if (!_dict.TryGetValue(entryKey, out var entry))
            throw new ArgumentException($"Entry key '{entryKey}' not found in the list.");
        if (!_dict.TryGetValue(referenceKey, out var refItem))
            throw new ArgumentException($"Reference key '{referenceKey}' not found in the list.");

        _list.Remove(entry);
        var refIdx = _list.IndexOf(refItem);
        var insertIdx = before ? refIdx : refIdx + 1;
        _list.Insert(insertIdx, entry);
    }

    /// <summary>
    /// Moves an existing entry to the first or last position.
    /// </summary>
    /// <param name="entryKey">Key of the entry to move.</param>
    /// <param name="first">If true, move to first position; if false, move to last.</param>
    public void Move(TKey entryKey, bool first)
    {
        if (!_dict.TryGetValue(entryKey, out var entry))
            throw new ArgumentException($"Entry key '{entryKey}' not found in the list.");

        _list.Remove(entry);
        if (first)
            _list.Insert(0, entry);
        else
            _list.Add(entry);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

