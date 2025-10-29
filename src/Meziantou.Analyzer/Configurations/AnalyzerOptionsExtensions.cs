using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Configurations;

public static class AnalyzerOptionsExtensions
{
    public static string GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, string defaultValue)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        if (configuration.TryGetValue(key, out var value))
            return value;

        return defaultValue;
    }

    public static string GetConfigurationValue(this AnalyzerOptions options, SyntaxNode syntaxNode, string key, string defaultValue)
    {
        return GetConfigurationValue(options, syntaxNode.SyntaxTree, key, defaultValue);
    }

    public static string GetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, string defaultValue)
    {
        return GetConfigurationValue(options, operation.Syntax.SyntaxTree, key, defaultValue);
    }

    public static bool GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, bool defaultValue)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        if (configuration.TryGetValue(key, out var value))
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
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        if (configuration.TryGetValue(key, out var value))
            return ChangeType(value, defaultValue);

        return defaultValue;
    }

    public static int GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, int defaultValue)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        if (configuration.TryGetValue(key, out var value))
            return ChangeType(value, defaultValue);

        return defaultValue;
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static ReportDiagnostic? GetConfigurationValue(this AnalyzerOptions options, SyntaxTree syntaxTree, string key, ReportDiagnostic? defaultValue)
    {
        var configuration = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        if (configuration.TryGetValue(key, out var value))
        {
            if (value is not null && Enum.TryParse<ReportDiagnostic>(value, ignoreCase: true, out var result))
                return result;
        }

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

    public static bool TryGetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, [NotNullWhen(true)] out string? value)
    {
        return TryGetConfigurationValue(options, operation.Syntax.SyntaxTree, key, out value);
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
