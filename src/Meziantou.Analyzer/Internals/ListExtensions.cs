using System.Collections.Generic;

namespace Meziantou.Analyzer
{
    internal static class ListExtensions
    {
        public static void AddIfNotNull<T>(this IList<T> list, IEnumerable<T> items) where T : class
        {
            foreach (var item in items)
            {
                AddIfNotNull(list, item);
            }
        }

        public static void AddIfNotNull<T>(this IList<T> list, T? item) where T : class
        {
            if (item is null)
                return;

            list.Add(item);
        }
    }
}
