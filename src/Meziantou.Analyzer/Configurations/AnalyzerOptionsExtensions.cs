using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Configurations;

public static class AnalyzerOptionsExtensions
{
    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        if (TryGetConfigurationValue(options, syntaxTree, configuration, out var value))
        {
            return ChangeType(value, configuration);
        }

        return configuration.DefaultValue;
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration, T defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, configuration, out var value))
            return ChangeType(value, configuration.Key, defaultValue);

        return defaultValue;
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxNode syntaxNode, ConfigurationDefinition<T> configuration)
    {
        return GetConfigurationValue(options, syntaxNode.SyntaxTree, configuration);
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, IOperation operation, ConfigurationDefinition<T> configuration)
    {
        return GetConfigurationValue(options, operation.Syntax.SyntaxTree, configuration);
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, IOperation operation, ConfigurationDefinition<T> configuration, T defaultValue)
    {
        return GetConfigurationValue(options, operation.Syntax.SyntaxTree, configuration, defaultValue);
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<T> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        if (options.TryGetConfigurationValue(symbol, configuration, out var value))
            return ChangeType(value, configuration);

        return configuration.DefaultValue;
    }

    /// <summary>
    /// Gets the <see cref="Regex"/> of an option whose value is a regular expression (<see cref="ConfigurationDefinition{T}.IsRegex"/>).
    /// The regex is created with the <see cref="ConfigurationDefinition{T}.RegexOptions"/> of the option and cached, so a value is
    /// parsed once until its entry is evicted from the cache. Returns <see langword="false"/> when the option is not configured,
    /// when its value is empty, or when its value is not a valid pattern. MA0220 reports the invalid patterns, so the rules can
    /// ignore them.
    /// </summary>
    public static bool TryGetConfigurationRegex(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<string> configuration, [NotNullWhen(true)] out Regex? regex)
    {
        if (TryGetConfigurationValue(options, syntaxTree, configuration, out var pattern) && pattern.Length > 0)
            return RegexCache.TryGetOrCreate(pattern, configuration.RegexOptions, out regex);

        regex = null;
        return false;
    }

    /// <inheritdoc cref="TryGetConfigurationRegex(AnalyzerOptions, SyntaxTree, ConfigurationDefinition{string}, out Regex?)"/>
    public static bool TryGetConfigurationRegex(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<string> configuration, [NotNullWhen(true)] out Regex? regex)
    {
        if (TryGetConfigurationValue(options, symbol, configuration, out var pattern) && pattern.Length > 0)
            return RegexCache.TryGetOrCreate(pattern, configuration.RegexOptions, out regex);

        regex = null;
        return false;
    }

    /// <summary>
    /// Gets the <see cref="Regex"/> of an option whose value is a regular expression, or the <see cref="Regex"/> of the default value
    /// of the option when it is not configured or when its value is not a valid pattern. Returns <see langword="null"/> when the
    /// default value itself is not a valid pattern.
    /// </summary>
    public static Regex? GetConfigurationRegex(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<string> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        if (TryGetConfigurationRegex(options, symbol, configuration, out var regex))
            return regex;

        RegexCache.TryGetOrCreate(configuration.DefaultValue, configuration.RegexOptions, out regex);
        return regex;
    }

    public static bool TryGetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, [NotNullWhen(true)] out string? value)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return configuration.TryGetValue(key, out value);
    }

    public static bool TryGetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration, [NotNullWhen(true)] out string? value)
    {
        foreach (var key in configuration.Keys)
        {
            if (TryGetConfigurationValue(options, syntaxTree, key, out value))
                return true;
        }

        value = null;
        return false;
    }

    public static bool TryGetConfigurationValue<T>(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<T> configuration, [NotNullWhen(true)] out string? value)
    {
        foreach (var key in configuration.Keys)
        {
            if (TryGetConfigurationValue(options, symbol, key, out value))
                return true;
        }

        value = null;
        return false;
    }

    public static bool TryGetConfigurationValue(this AnalyzerOptions options, ISymbol symbol, string key, [NotNullWhen(true)] out string? value)
    {
        foreach (var location in symbol.Locations)
        {
            var syntaxTree = location.SourceTree;
            if (syntaxTree is not null && options.TryGetConfigurationValue(syntaxTree, key, out value))
                return true;
        }

        value = null;
        return false;
    }

    private static bool ChangeType(string value, bool defaultValue)
    {
        if (value is not null && bool.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    private static int ChangeType(string value, int defaultValue)
    {
        if (value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;

        return defaultValue;
    }

    private static T ChangeType<T>(string value, ConfigurationDefinition<T> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        return ChangeType(value, configuration.Key, configuration.DefaultValue);
    }

    private static T ChangeType<T>(string value, string configurationKey, T defaultValue)
    {
        if (typeof(T) == typeof(bool))
        {
            if (defaultValue is bool boolDefaultValue)
                return (T)(object)ChangeType(value, boolDefaultValue);

            throw new InvalidOperationException($"Configuration value for '{configurationKey}' has an invalid default value.");
        }

        if (typeof(T) == typeof(int))
        {
            if (defaultValue is int intDefaultValue)
                return (T)(object)ChangeType(value, intDefaultValue);

            throw new InvalidOperationException($"Configuration value for '{configurationKey}' has an invalid default value.");
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(ReportDiagnostic?))
        {
            if (value is not null && Enum.TryParse<ReportDiagnostic>(value, ignoreCase: true, out var result))
                return (T)(object)result;

            return defaultValue;
        }

        throw new NotSupportedException($"Configuration value for '{configurationKey}' has an unsupported type '{typeof(T)}'.");
    }
}
