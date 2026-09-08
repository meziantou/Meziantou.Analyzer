using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class ObjectPoolTests
{
    [Fact]
    public void QueuePooledObjectPolicy_ClearsTheQueueOnReturn()
    {
        var policy = new QueuePooledObjectPolicy<object>();
        var queue = policy.Create();
        queue.Enqueue(new object());

        Assert.True(policy.Return(queue));
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void QueuePooledObjectPolicy_DoesNotRetainOversizedQueues()
    {
        var policy = new QueuePooledObjectPolicy<object> { MaximumRetainedCount = 1 };
        var queue = policy.Create();
        queue.Enqueue(new object());
        queue.Enqueue(new object());

        Assert.False(policy.Return(queue));
    }

    [Fact]
    public void QueuePooledObjectPolicy_DoesNotRetainOversizedQueuesThatWereDrained()
    {
        var policy = new QueuePooledObjectPolicy<object> { MaximumRetainedCount = 1 };
        var queue = policy.Create();
        queue.Enqueue(new object());
        queue.Enqueue(new object());
        while (queue.TryDequeue(out _))
        {
        }

        Assert.Equal(0, queue.Count);
        Assert.False(policy.Return(queue));
    }

    [Fact]
    public void QueuePooledObjectPolicy_ResetsTheMaximumCountOnReturn()
    {
        var policy = new QueuePooledObjectPolicy<object> { MaximumRetainedCount = 1 };
        var queue = policy.Create();
        queue.Enqueue(new object());

        Assert.True(policy.Return(queue));
        Assert.Equal(0, queue.MaximumCount);
        Assert.True(policy.Return(queue));
    }

    [Fact]
    public void QueuePool_DoesNotKeepTheItemsAlive()
    {
        var pool = ObjectPool.CreateQueuePool<object>();
        var queue = pool.Get();
        queue.Enqueue(new object());
        pool.Return(queue);

        Assert.Equal(0, pool.Get().Count);
    }

    [Fact]
    public void QueuePool_DoesNotReuseADrainedOversizedQueue()
    {
        var pool = ObjectPool.Create(new QueuePooledObjectPolicy<object> { MaximumRetainedCount = 1 });
        var queue = pool.Get();
        queue.Enqueue(new object());
        queue.Enqueue(new object());
        while (queue.TryDequeue(out _))
        {
        }

        pool.Return(queue);

        Assert.NotSame(queue, pool.Get());
    }
}
