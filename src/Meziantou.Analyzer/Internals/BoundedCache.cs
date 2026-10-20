using System.Collections.Concurrent;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// A thread-safe cache that keeps at most <see cref="Capacity"/> entries, so the values that are no longer used do not stay
/// alive for the lifetime of the process. An entry can be removed at any time, so the values must be reproducible from their key.
/// <para>
/// The entries are split in two generations of half the capacity. The entries are added to the current generation and, when it
/// is full, it becomes the previous generation and the entries of the former previous generation are removed. An entry of the
/// previous generation that is used is added back to the current generation, so the entries that are used often are kept.
/// Unlike a least recently used cache, getting an entry of the current generation writes nothing, so the lookups that run
/// concurrently do not contend, and removing the entries does not require sorting them.
/// </para>
/// </summary>
internal sealed class BoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly Lock _rotationLock = new();
    private readonly int _generationCapacity;
    private ConcurrentDictionary<TKey, TValue> _currentGeneration = new();
    private ConcurrentDictionary<TKey, TValue> _previousGeneration = new();

    // ConcurrentDictionary.Count takes all the locks of the dictionary, so the additions are counted instead
    private int _currentGenerationCount;

    public BoundedCache(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, message: "The capacity must be greater than 0");

        Capacity = capacity;

        // The generations are rotated once the current one is full, so the previous generation is full and the current one is not
        _generationCapacity = (capacity + 1) / 2;
    }

    /// <summary>
    /// Gets the maximum number of entries kept by the cache. The cache can hold a few more entries for a short time,
    /// when entries are added while the generations are rotated.
    /// </summary>
    public int Capacity { get; }

    /// <summary>Gets the number of entries of the generations. An entry used in both generations is counted twice.</summary>
    public int Count => Volatile.Read(ref _currentGeneration).Count + Volatile.Read(ref _previousGeneration).Count;

    /// <summary>
    /// Gets the value associated with the key, or adds the value created by <paramref name="valueFactory"/>.
    /// The factory can be invoked concurrently for the same key, and its value is then discarded.
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        var currentGeneration = Volatile.Read(ref _currentGeneration);
        if (currentGeneration.TryGetValue(key, out var value))
            return value;

        if (!Volatile.Read(ref _previousGeneration).TryGetValue(key, out value))
        {
            value = valueFactory(key);
        }

        if (!currentGeneration.TryAdd(key, value))
            return currentGeneration.TryGetValue(key, out var existingValue) ? existingValue : value;

        if (Interlocked.Increment(ref _currentGenerationCount) >= _generationCapacity)
        {
            Rotate(currentGeneration);
        }

        return value;
    }

    private void Rotate(ConcurrentDictionary<TKey, TValue> currentGeneration)
    {
        lock (_rotationLock)
        {
            // The generations may have been rotated by another thread in the meantime
            if (!ReferenceEquals(_currentGeneration, currentGeneration))
                return;

            // The entries added concurrently to the generation that becomes the previous one are kept until the next rotation,
            // so the cache can exceed its capacity by a few entries
            Volatile.Write(ref _previousGeneration, currentGeneration);
            Volatile.Write(ref _currentGeneration, new ConcurrentDictionary<TKey, TValue>());
            Volatile.Write(ref _currentGenerationCount, 0);
        }
    }
}
