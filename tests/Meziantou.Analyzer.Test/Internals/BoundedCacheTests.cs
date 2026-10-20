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
    public void TheEntriesThatAreNotUsedAreEvicted()
    {
        static int Fail(int key) => throw new InvalidOperationException("The entry should be cached");

        var cache = new BoundedCache<int, int>(capacity: 4);
        cache.GetOrAdd(0, static key => key);
        cache.GetOrAdd(1, static key => key);

        // Use every entry but the first one, then add an entry so the cache evicts the entries that are not used
        cache.GetOrAdd(1, Fail);
        cache.GetOrAdd(2, static key => key);

        cache.GetOrAdd(2, Fail);
        cache.GetOrAdd(1, Fail);

        var created = false;
        cache.GetOrAdd(0, key => { created = true; return key; });
        Assert.True(created);
    }

    [Fact]
    public void TheEntriesThatAreUsedOftenAreKept()
    {
        var cache = new BoundedCache<int, int>(capacity: 8);
        cache.GetOrAdd(-1, static key => key);
        for (var i = 0; i < 1000; i++)
        {
            cache.GetOrAdd(i, static key => key);
            Assert.Equal(-1, cache.GetOrAdd(-1, static key => throw new InvalidOperationException("The entry should be cached")));
        }
    }

    [Fact]
    public void TheEntriesCanBeUsedConcurrentlyWithTheEviction()
    {
        var cache = new BoundedCache<int, int>(capacity: 256);
        Parallel.For(0, Environment.ProcessorCount * 2, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, worker =>
        {
            var random = new Random(worker);
            for (var i = 0; i < 200_000; i++)
            {
                // Most of the lookups use the same entries, so their usage changes while the eviction sorts them
#pragma warning disable CA5394 // Do not use insecure randomness: the random numbers only simulate the lookups
                var key = random.Next(10) is 0 ? random.Next(100_000) : random.Next(256);
#pragma warning restore CA5394
                Assert.Equal(key, cache.GetOrAdd(key, static key => key));
            }
        });

        Assert.True(cache.Count <= cache.Capacity * 2, $"The cache contains {cache.Count} entries");
    }
}
