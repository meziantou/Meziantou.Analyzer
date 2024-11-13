using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Meziantou.Analyzer.Internals;

internal static class EnumerableExtensions
{
    [return: NotNullIfNotNull(parameterName: nameof(source))]
    public static IEnumerable<T>? WhereNotNull<T>(this IEnumerable<T?>? source) where T : class
    {
        if (source is null)
            return null;

        return source.Where(item => item is not null)!;
    }

    [return: MaybeNull]
    public static T SingleOrDefaultIfMultiple<T>(this IEnumerable<T> source)
    {
        using var iterator = source.GetEnumerator();
        try
        {
            if (iterator.MoveNext())
            {
                var result = iterator.Current;
                if (!iterator.MoveNext())
                    return result;
            }
        }
        finally
        {
            iterator.Dispose();
        }

        return default;
    }
}
