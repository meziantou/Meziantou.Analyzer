using System.Runtime.CompilerServices;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Compares the instances by reference, whatever the members they override. <c>ReferenceEqualityComparer</c> is not
/// available on every target framework, and the default comparer of an interface dispatches to the implementation.
/// </summary>
internal sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
{
    public static readonly ReferenceComparer<T> Instance = new();

    private ReferenceComparer()
    {
    }

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
