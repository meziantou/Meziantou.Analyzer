using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Internals;

internal static class RegexCache
{
    private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options, long TimeoutTicks), Regex> Cache = new();

    public static Regex GetOrCreate(string pattern, RegexOptions options, TimeSpan timeout)
    {
        return Cache.GetOrAdd((pattern, options, timeout.Ticks), static key => new Regex(key.Pattern, key.Options, TimeSpan.FromTicks(key.TimeoutTicks)));
    }
}
