namespace Meziantou.Analyzer.Internals;
internal static class HashSetExtensions
{
    public static void AddIfNotNull<T>(this HashSet<T> hashSet, T? item)
    {
        if (item is null)
            return;

        _ = hashSet.Add(item);
    }
}
