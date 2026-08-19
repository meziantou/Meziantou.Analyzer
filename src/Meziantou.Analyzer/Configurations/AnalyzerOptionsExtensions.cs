namespace Meziantou.Analyzer.Configurations;

public static class AnalyzerOptionsExtensions
{
    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration)
    {
        if (!configuration.HasDefaultValue)
            throw new InvalidOperationException($"Configuration value for '{configuration.Key}' is not set and has no default value.");

        if (TryGetConfigurationValue(options, syntaxTree, configuration.Key, out var value))
        {
            return ChangeType(value, configuration);
        }

        return configuration.DefaultValue;
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration, T defaultValue)
    {
        if (TryGetConfigurationValue(options, syntaxTree, configuration.Key, out var value))
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

        foreach (var location in symbol.Locations)
        {
            var syntaxTree = location.SourceTree;
            if (syntaxTree is not null && options.TryGetConfigurationValue(syntaxTree, configuration.Key, out var value))
                return ChangeType(value, configuration);
        }

        return configuration.DefaultValue;
    }

    public static T GetConfigurationValue<T>(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<T> configuration, T defaultValue)
    {
        foreach (var location in symbol.Locations)
        {
            var syntaxTree = location.SourceTree;
            if (syntaxTree is not null && options.TryGetConfigurationValue(syntaxTree, configuration.Key, out var value))
                return ChangeType(value, configuration.Key, defaultValue);
        }

        return defaultValue;
    }

    public static bool TryGetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, [NotNullWhen(true)] out string? value)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return configuration.TryGetValue(key, out value);
    }

    public static bool TryGetConfigurationValue<T>(this AnalyzerOptions options, SyntaxTree syntaxTree, ConfigurationDefinition<T> configuration, [NotNullWhen(true)] out string? value)
    {
        return TryGetConfigurationValue(options, syntaxTree, configuration.Key, out value);
    }

    public static bool TryGetConfigurationValue<T>(this AnalyzerOptions options, ISymbol symbol, ConfigurationDefinition<T> configuration, [NotNullWhen(true)] out string? value)
    {
        return TryGetConfigurationValue(options, symbol, configuration.Key, out value);
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
