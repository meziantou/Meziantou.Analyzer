using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Internals;

internal static class RegexCache
{
    // The patterns come from the configuration, so they can be invalid or subject to catastrophic backtracking.
    // The timeout ensures a single pattern cannot hang the compilation.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
    private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), (Regex? Regex, string? ErrorMessage)> Cache = new();

    /// <summary>
    /// Gets a cached <see cref="Regex"/> for the pattern. Returns <see langword="false"/> when the pattern is invalid.
    /// Invalid patterns are cached, so a pattern is parsed only once.
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
    /// Indicates whether the pattern matches the input. Returns <paramref name="defaultValue"/> when the pattern
    /// is invalid or when the evaluation times out.
    /// </summary>
    public static bool IsMatch(string pattern, RegexOptions options, string input, bool defaultValue)
    {
        if (!TryGetOrCreate(pattern, options, out var regex))
            return defaultValue;

        return IsMatch(regex, input, defaultValue);
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
