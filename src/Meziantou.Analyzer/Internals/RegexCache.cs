using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Internals;

internal static class RegexCache
{
    // The patterns come from the configuration, so they can be invalid or subject to catastrophic backtracking.
    // The timeout ensures a single pattern cannot hang the compilation.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    // The patterns come from the configuration of the analyzed projects, and an editor provides a new value at every
    // keystroke, including the invalid intermediate ones. The cache is bounded, so the patterns of the closed projects
    // and of the outdated configuration values do not stay alive for the lifetime of the process.
    private static readonly BoundedCache<(string Pattern, RegexOptions Options), (Regex? Regex, string? ErrorMessage)> Cache = new(capacity: 128);

    /// <summary>
    /// Gets a cached <see cref="Regex"/> for the pattern. Returns <see langword="false"/> when the pattern is invalid.
    /// Invalid patterns are cached too, so a pattern is parsed once until its entry is evicted from the cache.
    /// </summary>
    public static bool TryGetOrCreate(string pattern, RegexOptions options, [NotNullWhen(true)] out Regex? regex)
    {
        regex = GetOrCreate(pattern, options).Regex;
        return regex is not null;
    }

    /// <summary>
    /// Indicates whether the pattern is a valid regular expression. <paramref name="errorMessage"/> contains
    /// the reason why the pattern is invalid.
    /// </summary>
    public static bool IsValidPattern(string pattern, RegexOptions options, [NotNullWhen(false)] out string? errorMessage)
    {
        var (regex, message) = GetOrCreate(pattern, options);
        errorMessage = message;
        return regex is not null;
    }

    /// <summary>
    /// Indicates whether the regex matches the input. Returns <paramref name="defaultValue"/> when the evaluation times out.
    /// </summary>
    public static bool IsMatch(Regex regex, string input, bool defaultValue)
    {
        try
        {
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Replaces all the matches of the regex in the input. Returns <paramref name="defaultValue"/> when the evaluation times out.
    /// </summary>
    public static string Replace(Regex regex, string input, string replacement, string defaultValue)
    {
        try
        {
            return regex.Replace(input, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return defaultValue;
        }
    }

    private static (Regex? Regex, string? ErrorMessage) GetOrCreate(string pattern, RegexOptions options)
    {
        return Cache.GetOrAdd((pattern, options), static key =>
        {
            try
            {
                return (new Regex(key.Pattern, key.Options, MatchTimeout), null);
            }
            catch (ArgumentException ex)
            {
                return (null, ex.Message);
            }
        });
    }
}
