using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class BoundedCacheTests
{
    [Fact]
    public void CreatingACacheWithAnInvalidCapacityThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedCache<string, string>(capacity: 0));
    }

    [Fact]
    public void TheValueIsCreatedOncePerKey()
    {
        var invocations = 0;
        var cache = new BoundedCache<string, string>(capacity: 4);

        string Create(string key)
        {
            invocations++;
            return key + "!";
        }

        Assert.Equal("a!", cache.GetOrAdd("a", Create));
        Assert.Equal("a!", cache.GetOrAdd("a", Create));
        Assert.Equal("b!", cache.GetOrAdd("b", Create));

        Assert.Equal(2, invocations);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void TheNumberOfEntriesIsBounded()
    {
        var cache = new BoundedCache<int, int>(capacity: 8);

        for (var i = 0; i < 1000; i++)
        {
            cache.GetOrAdd(i, static key => key);
            Assert.True(cache.Count <= cache.Capacity, $"The cache contains {cache.Count} entries after adding {i + 1} keys");
        }
    }

    [Fact]
    public void TheLeastRecentlyUsedEntriesAreEvicted()
    {
        static int Fail(int key) => throw new InvalidOperationException("The entry should be cached");

        var cache = new BoundedCache<int, int>(capacity: 4);
        for (var i = 0; i < 4; i++)
        {
            cache.GetOrAdd(i, static key => key);
        }

        // Use every entry but the first one, then add an entry so the cache evicts the least recently used entries
        for (var i = 1; i < 4; i++)
        {
            cache.GetOrAdd(i, Fail);
        }

        cache.GetOrAdd(4, static key => key);

        cache.GetOrAdd(4, Fail);
        cache.GetOrAdd(3, Fail);
        cache.GetOrAdd(2, Fail);

        var created = false;
        cache.GetOrAdd(0, key => { created = true; return key; });
        Assert.True(created);
    }
}
