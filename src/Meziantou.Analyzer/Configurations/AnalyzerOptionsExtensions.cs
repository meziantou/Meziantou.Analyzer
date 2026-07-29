using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Configurations;

public static class AnalyzerOptionsExtensions
{
    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        if (TryGetConfigurationValue(options, syntaxTree, configuration.Key, out var value))
        {
            if(typeof(T) == typeof(bool))
            {
                return (T)(object)ChangeType(value, (bool)(object)configuration.DefaultValue);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)ChangeType(value, (int)(object)configuration.DefaultValue);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(ReportDiagnostic?))
            {
                if (value is not null && Enum.TryParse<ReportDiagnostic>(value, ignoreCase: true, out var result))
                    return (T)(object)result;
            }
            else
            {
                throw new NotSupportedException($"Configuration value for '{configuration.Key}' has an unsupported type '{typeof(T)}'.");
            }
        }

        return configuration.DefaultValue;
    }

    public static string GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, string defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, key, out var value))
            return value;

        return defaultValue;
    }

    public static string GetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, string defaultValue)
    {
        return GetConfigurationValue(options, operation.Syntax.SyntaxTree, key, defaultValue);
    }

    public static bool GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, bool defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, key, out var value))
            return ChangeType(value, defaultValue);

        return defaultValue;
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static bool? GetConfigurationValue(this AnalyzerOptions options, SyntaxNode syntaxNode, string key, bool? defaultValue)
    {
        return GetConfigurationValue(options, syntaxNode.SyntaxTree, key, defaultValue);
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static bool? GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, bool? defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, key, out var value))
            return ChangeType(value, defaultValue);

        return defaultValue;
    }

    public static int GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, int defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, key, out var value))
            return ChangeType(value, defaultValue);

        return defaultValue;
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetConfigurationValue(this AnalyzerOptions options, ISymbol symbol, string key, string? defaultValue)
    {
        foreach (var location in symbol.Locations)
        {
            var syntaxTree = location.SourceTree;
            if (syntaxTree is not null && options.TryGetConfigurationValue(syntaxTree, key, out var str))
                return str;
        }

        return defaultValue;
    }

    public static bool GetConfigurationValue(this AnalyzerOptions options, ISymbol symbol, string key, bool defaultValue)
    {
        foreach (var location in symbol.Locations)
        {
            var syntaxTree = location.SourceTree;
            if (syntaxTree is not null && options.TryGetConfigurationValue(syntaxTree, key, out var str))
                return ChangeType(str, defaultValue);
        }

        return defaultValue;
    }

    public static bool TryGetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, [NotNullWhen(true)] out string? value)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return configuration.TryGetValue(key, out value);
    }

    public static bool GetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, bool defaultValue)
    {
        return GetConfigurationValue(options, operation.Syntax.SyntaxTree, key, defaultValue);
    }

    private static bool ChangeType(string value, bool defaultValue)
    {
        if (value is not null && bool.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    private static bool? ChangeType(string value, bool? defaultValue)
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
}
