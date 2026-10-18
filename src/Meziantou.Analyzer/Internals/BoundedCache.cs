using System.Collections.Concurrent;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// A thread-safe cache that keeps at most <see cref="Capacity"/> entries. When the capacity is exceeded, the least
/// recently used entries are removed, so the values that are no longer used do not stay alive for the lifetime of
/// the process. An entry can be removed at any time, so the values must be reproducible from their key.
/// </summary>
internal sealed class BoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Entry> _entries = new();
    private readonly Lock _evictionLock = new();
    private long _lastUsage;

    // ConcurrentDictionary.Count takes all the locks of the dictionary, so the additions are counted to only read it when the cache can be full
    private int _count;

    public BoundedCache(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, message: "The capacity must be greater than 0");

        Capacity = capacity;
    }

    /// <summary>
    /// Gets the maximum number of entries kept by the cache. The cache can hold a few more entries for a short time,
    /// as the eviction happens once an entry is added.
    /// </summary>
    public int Capacity { get; }

    public int Count => _entries.Count;

    /// <summary>
    /// Gets the value associated with the key, or adds the value created by <paramref name="valueFactory"/>.
    /// The factory can be invoked concurrently for the same key, and its value is then discarded.
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            var newEntry = new Entry(valueFactory(key));
            entry = _entries.GetOrAdd(key, newEntry);
            Touch(entry);
            if (ReferenceEquals(entry, newEntry) && Interlocked.Increment(ref _count) > Capacity)
            {
                Evict();
            }

            return entry.Value;
        }

        Touch(entry);
        return entry.Value;
    }

    private void Touch(Entry entry) => entry.LastUsage = Interlocked.Increment(ref _lastUsage);

    private void Evict()
    {
        lock (_evictionLock)
        {
            if (_entries.Count > Capacity)
            {
                // Remove a quarter of the entries, so the eviction does not run at every addition once the cache is full.
                // The entries added while the snapshot is taken can be removed, which is fine as their value is recreated on demand.
                var entries = _entries.ToArray();

                // The entries are used concurrently, so their usage is read once before sorting them. Otherwise, the comparisons are inconsistent and Array.Sort throws.
                var lastUsages = new long[entries.Length];
                for (var i = 0; i < entries.Length; i++)
                {
                    lastUsages[i] = entries[i].Value.LastUsage;
                }

                Array.Sort(lastUsages, entries);

                var retainedCount = Math.Max(1, Capacity - (Capacity / 4));
                for (var i = 0; i < entries.Length - retainedCount; i++)
                {
                    _entries.TryRemove(entries[i].Key, out _);
                }
            }

            // The entries added in the meantime may not be counted, so the cache can exceed its capacity by a few entries until the next eviction
            Volatile.Write(ref _count, _entries.Count);
        }
    }

    private sealed class Entry(TValue value)
    {
        private long _lastUsage;

        public TValue Value { get; } = value;

        /// <summary>
        /// Gets or sets the value of the counter of the cache when the entry was last used. The accessors are
        /// interlocked, so the value cannot tear when the entry is used concurrently.
        /// </summary>
        public long LastUsage
        {
            get => Interlocked.Read(ref _lastUsage);
            set => Interlocked.Exchange(ref _lastUsage, value);
        }
    }
}
