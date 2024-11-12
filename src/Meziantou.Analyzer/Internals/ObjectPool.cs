#pragma warning disable MA0048 // File name must match type name
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// A pool of objects.
/// </summary>
/// <typeparam name="T">The type of objects to pool.</typeparam>
internal abstract class ObjectPool<T> where T : class
{
    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T"/>.</returns>
    public abstract T Get();

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public abstract void Return(T obj);
}

/// <summary>
/// Methods for creating <see cref="ObjectPool{T}"/> instances.
/// </summary>
internal static class ObjectPool
{
    public static ObjectPool<StringBuilder> SharedStringBuilderPool { get; } = CreateStringBuilderPool();

    public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T>? policy = null) where T : class, new()
    {
        var provider = new DefaultObjectPoolProvider();
        return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
    }

    public static ObjectPool<StringBuilder> CreateStringBuilderPool()
    {
        var provider = new DefaultObjectPoolProvider();
        return provider.Create(new StringBuilderPooledObjectPolicy());
    }
}

/// <summary>
/// Represents a policy for managing pooled objects.
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
internal interface IPooledObjectPolicy<T> where T : notnull
{
    /// <summary>
    /// Create a <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The <typeparamref name="T"/> which was created.</returns>
    T Create();

    /// <summary>
    /// Runs some processing when an object was returned to the pool. Can be used to reset the state of an object and indicate if the object should be returned to the pool.
    /// </summary>
    /// <param name="obj">The object to return to the pool.</param>
    /// <returns><see langword="true" /> if the object should be returned to the pool. <see langword="false" /> if it's not possible/desirable for the pool to keep the object.</returns>
    bool Return(T obj);
}

/// <summary>
/// A provider of <see cref="ObjectPool{T}"/> instances.
/// </summary>
internal abstract class ObjectPoolProvider
{
    /// <summary>
    /// Creates an <see cref="ObjectPool"/>.
    /// </summary>
    /// <typeparam name="T">The type to create a pool for.</typeparam>
    public ObjectPool<T> Create<T>() where T : class, new()
    {
        return Create<T>(new DefaultPooledObjectPolicy<T>());
    }

    /// <summary>
    /// Creates an <see cref="ObjectPool"/> with the given <see cref="IPooledObjectPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to create a pool for.</typeparam>
    public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
}

/// <summary>
/// Default implementation of <see cref="ObjectPool{T}"/>.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be Garbage Collected.</remarks>
internal class DefaultObjectPool<T> : ObjectPool<T> where T : class
{
    private readonly Func<T> _createFunc;
    private readonly Func<T, bool> _returnFunc;
    private readonly int _maxCapacity;
    private int _numItems;

    private protected readonly ConcurrentQueue<T> Items = new();
    private protected T? FastItem;

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    public DefaultObjectPool(IPooledObjectPolicy<T> policy)
        : this(policy, Environment.ProcessorCount * 2)
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        // cache the target interface methods, to avoid interface lookup overhead
        _createFunc = policy.Create;
        _returnFunc = policy.Return;
        _maxCapacity = maximumRetained - 1;  // -1 to account for _fastItem
    }

    /// <inheritdoc />
    public override T Get()
    {
        var item = FastItem;
        if (item == null || Interlocked.CompareExchange(ref FastItem, null, item) != item)
        {
            if (Items.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _numItems);
                return item;
            }

            // no object available, so go get a brand new one
            return _createFunc();
        }

        return item;
    }

    /// <inheritdoc />
    public override void Return(T obj)
    {
        ReturnCore(obj);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>true if the object was returned to the pool</returns>
    private protected bool ReturnCore(T obj)
    {
        if (!_returnFunc(obj))
        {
            // policy says to drop this object
            return false;
        }

        if (FastItem != null || Interlocked.CompareExchange(ref FastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                Items.Enqueue(obj);
                return true;
            }

            // no room, clean up the count and drop the object on the floor
            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }
}

/// <summary>
/// The default <see cref="ObjectPoolProvider"/>.
/// </summary>
internal sealed class DefaultObjectPoolProvider : ObjectPoolProvider
{
    /// <summary>
    /// The maximum number of objects to retain in the pool.
    /// </summary>
    public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            return new DisposableObjectPool<T>(policy, MaximumRetained);
        }

        return new DefaultObjectPool<T>(policy, MaximumRetained);
    }
}

/// <summary>
/// Default implementation for <see cref="PooledObjectPolicy{T}"/>.
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
internal sealed class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
{
    /// <inheritdoc />
    public override T Create()
    {
        return new T();
    }

    /// <inheritdoc />
    public override bool Return(T obj)
    {
        if (obj is IResettable resettable)
        {
            return resettable.TryReset();
        }

        return true;
    }
}

/// <summary>
/// A base type for <see cref="IPooledObjectPolicy{T}"/>.
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
internal abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : notnull
{
    /// <inheritdoc />
    public abstract T Create();

    /// <inheritdoc />
    public abstract bool Return(T obj);
}

/// <summary>
/// Defines a method to reset an object to its initial state.
/// </summary>
internal interface IResettable
{
    /// <summary>
    /// Reset the object to a neutral state, semantically similar to when the object was first constructed.
    /// </summary>
    /// <returns><see langword="true" /> if the object was able to reset itself, otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// In general, this method is not expected to be thread-safe.
    /// </remarks>
    bool TryReset();
}

internal sealed class DisposableObjectPool<T> : DefaultObjectPool<T>, IDisposable where T : class
{
    private volatile bool _isDisposed;

    public DisposableObjectPool(IPooledObjectPolicy<T> policy)
        : base(policy)
    {
    }

    public DisposableObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        : base(policy, maximumRetained)
    {
    }

    public override T Get()
    {
        if (_isDisposed)
        {
            ThrowObjectDisposedException();
        }

        return base.Get();

        void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public override void Return(T obj)
    {
        // When the pool is disposed or the obj is not returned to the pool, dispose it
        if (_isDisposed || !ReturnCore(obj))
        {
            DisposeItem(obj);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;

        DisposeItem(FastItem);
        FastItem = null;

        while (Items.TryDequeue(out var item))
        {
            DisposeItem(item);
        }
    }

    private static void DisposeItem(T? item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// A policy for pooling <see cref="StringBuilder"/> instances.
/// </summary>
internal sealed class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
{
    /// <summary>
    /// Gets or sets the initial capacity of pooled <see cref="StringBuilder"/> instances.
    /// </summary>
    /// <value>Defaults to <c>100</c>.</value>
    public int InitialCapacity { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum value for <see cref="StringBuilder.Capacity"/> that is allowed to be
    /// retained, when <see cref="Return(StringBuilder)"/> is invoked.
    /// </summary>
    /// <value>Defaults to <c>4096</c>.</value>
    public int MaximumRetainedCapacity { get; set; } = 4 * 1024;

    /// <inheritdoc />
    public override StringBuilder Create()
    {
        return new StringBuilder(InitialCapacity);
    }

    /// <inheritdoc />
    public override bool Return(StringBuilder obj)
    {
        if (obj.Capacity > MaximumRetainedCapacity)
        {
            // Too big. Discard this one.
            return false;
        }

        obj.Clear();
        return true;
    }
}