using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Configurations
{
    public static class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, ConfigurationHierarchy> s_cachedOptions
            = new();

        public static bool GetConfigurationValue(this AnalyzerOptions options, string filePath, string key, bool defaultValue)
        {
            var configuration = GetConfigurationHierarchy(options);
            if (configuration.TryGetValue(filePath, key, out var value))
            {
                return ChangeType(value, defaultValue);
            }

            return defaultValue;
        }

        public static bool? GetConfigurationValue(this AnalyzerOptions options, string filePath, string key, bool? defaultValue)
        {
            var configuration = GetConfigurationHierarchy(options);
            if (configuration.TryGetValue(filePath, key, out var value))
            {
                return ChangeType(value, defaultValue);
            }

            return defaultValue;
        }

        public static int GetConfigurationValue(this AnalyzerOptions options, string filePath, string key, int defaultValue)
        {
            var configuration = GetConfigurationHierarchy(options);
            if (configuration.TryGetValue(filePath, key, out var value))
            {
                return ChangeType(value, defaultValue);
            }

            return defaultValue;
        }

        public static ReportDiagnostic? GetConfigurationValue(this AnalyzerOptions options, string filePath, string key, ReportDiagnostic? defaultValue)
        {
            var configuration = GetConfigurationHierarchy(options);
            if (configuration.TryGetValue(filePath, key, out var value))
            {
                if (value != null && Enum.TryParse<ReportDiagnostic>(value, ignoreCase: true, out var result))
                    return result;
            }

            return defaultValue;
        }

        public static bool GetConfigurationValue(this AnalyzerOptions options, ISymbol symbol, string key, bool defaultValue)
        {
            foreach (var location in symbol.Locations)
            {
                var filePath = location.SourceTree?.FilePath;
                if (filePath != null && options.TryGetConfigurationValue(filePath, key, out var str))
                    return ChangeType(str, defaultValue);
            }

            return defaultValue;
        }

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, string filePath, string key, [NotNullWhen(true)] out string? value)
        {
            var configuration = GetConfigurationHierarchy(options);
            return configuration.TryGetValue(filePath, key, out value);
        }

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, [NotNullWhen(true)] out string? value)
        {
            return TryGetConfigurationValue(options, operation.Syntax, key, out value);
        }

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, SyntaxNode syntaxNode, string key, [NotNullWhen(true)] out string? value)
        {
            return TryGetConfigurationValue(options, syntaxNode.SyntaxTree.FilePath, key, out value);
        }

        private static ConfigurationHierarchy GetConfigurationHierarchy(this AnalyzerOptions options)
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists. 
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            var createValueCallback = new ConditionalWeakTable<AnalyzerOptions, ConfigurationHierarchy>.CreateValueCallback(_ => new ConfigurationHierarchy(GetFromAdditionalFiles()));
            return s_cachedOptions.GetValue(options, createValueCallback);

            EditorConfigFile GetFromAdditionalFiles()
            {
                foreach (var additionalFile in options.AdditionalFiles)
                {
                    var fileName = Path.GetFileName(additionalFile.Path);
                    if (fileName.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = additionalFile.GetText();
                        if (text != null)
                            return EditorConfigFileParser.Parse(text);
                    }
                }

                return EditorConfigFile.Empty;
            }
        }

        private static bool ChangeType(string value, bool defaultValue)
        {
            if (value != null && bool.TryParse(value, out var result))
                return result;

            return defaultValue;
        }

        private static bool? ChangeType(string value, bool? defaultValue)
        {
            if (value != null && bool.TryParse(value, out var result))
                return result;

            return defaultValue;
        }

        private static int ChangeType(string value, int defaultValue)
        {
            if (value != null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;

            return defaultValue;
        }
    }
}
