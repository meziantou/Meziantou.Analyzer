using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Configurations
{
    public static class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, ConfigurationHierarchy> s_cachedOptions
            = new ConditionalWeakTable<AnalyzerOptions, ConfigurationHierarchy>();

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, string filePath, string key, out string value)
        {
            var configuration = GetConfigurationHierarchy(options);
            return configuration.TryGetValue(filePath, key, out value);
        }

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, IOperation operation, string key, out string value)
        {
            return TryGetConfigurationValue(options, operation.Syntax, key, out value);
        }

        public static bool TryGetConfigurationValue(this AnalyzerOptions options, SyntaxNode syntaxNode, string key, out string value)
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
                        return EditorConfigFileParser.Parse(text);
                    }
                }

                return EditorConfigFile.Empty;
            }
        }
    }
}
