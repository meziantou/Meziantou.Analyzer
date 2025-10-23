using System.Collections.Concurrent;
using System.Collections;

namespace Meziantou.Analyzer.Internals;
internal sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary;

    public ConcurrentHashSet()
    {
        _dictionary = new ConcurrentDictionary<T, byte>();
    }

    public ConcurrentHashSet(IEqualityComparer<T> equalityComparer)
    {
        _dictionary = new ConcurrentDictionary<T, byte>(equalityComparer);
    }

    public int Count => _dictionary.Count;

    public bool IsEmpty => _dictionary.IsEmpty;

    public bool IsReadOnly => false;

    public bool Contains(T item) => _dictionary.ContainsKey(item);

    public bool Add(T value) => _dictionary.TryAdd(value, 0);

    public void AddRange(IEnumerable<T>? values)
    {
        if (values is not null)
        {
            foreach (var v in values)
            {
                Add(v);
            }
        }
    }

    public bool Remove(T item) => _dictionary.TryRemove(item, out _);

    public void Clear() => _dictionary.Clear();

    public readonly struct KeyEnumerator : IEnumerator<T>
    {
        private readonly IEnumerator<KeyValuePair<T, byte>> _kvpEnumerator;

        internal KeyEnumerator(IEnumerable<KeyValuePair<T, byte>> data)
        {
            _kvpEnumerator = data.GetEnumerator();
        }

        public T Current => _kvpEnumerator.Current.Key;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _kvpEnumerator.MoveNext();
        }

        public void Reset()
        {
            _kvpEnumerator.Reset();
        }

        public void Dispose()
        {
            _kvpEnumerator.Dispose();
        }
    }

    public KeyEnumerator GetEnumerator()
    {
        // PERF: Do not use dictionary.Keys here because that creates a snapshot
        // of the collection resulting in a List<T> allocation. Instead, use the
        // KeyValuePair enumerator and pick off the Key part.
        return new KeyEnumerator(_dictionary);
    }

    private IEnumerator<T> GetEnumeratorImpl()
    {
        // PERF: Do not use dictionary.Keys here because that creates a snapshot
        // of the collection resulting in a List<T> allocation. Instead, use the
        // KeyValuePair enumerator and pick off the Key part.
        foreach (var kvp in _dictionary)
        {
            yield return kvp.Key;
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumeratorImpl();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorImpl();

    void ICollection<T>.Add(T item) => Add(item);

    public void CopyTo(T[] array, int arrayIndex)
    {
        // PERF: Do not use dictionary.Keys here because that creates a snapshot
        // of the collection resulting in a List<T> allocation.
        // Instead, enumerate the set and copy over the elements.
        foreach (var element in this)
        {
            array[arrayIndex++] = element;
        }
    }
}
