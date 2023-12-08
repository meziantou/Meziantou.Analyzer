using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Meziantou.Analyzer;

internal static class EnumerableExtensions
{
    [return: MaybeNull]
    public static TSource SingleOrDefaultIfMultiple<TSource>(this IEnumerable<TSource> source)
    {
        var elements = source.Take(2).ToArray();

        return (elements.Length == 1) ? elements[0] : default;
    }

    [return: NotNullIfNotNull(parameterName: nameof(source))]
    public static IEnumerable<T>? WhereNotNull<T>(this IEnumerable<T?>? source) where T : class
    {
        if (source is null)
            return null;

        return source.Where(item => item is not null)!;
    }
}
