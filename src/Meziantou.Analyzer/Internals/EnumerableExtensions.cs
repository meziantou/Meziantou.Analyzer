using System.Collections.Generic;
using System.Linq;

namespace Meziantou.Analyzer
{
    internal static class EnumerableExtensions
    {
        public static TSource SingleOrDefaultIfMultiple<TSource>(this IEnumerable<TSource> source)
        {
            var elements = source.Take(2).ToArray();

            return (elements.Length == 1) ? elements[0] : default;
        }
    }
}
