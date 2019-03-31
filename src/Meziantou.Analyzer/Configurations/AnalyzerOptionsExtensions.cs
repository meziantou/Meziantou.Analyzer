using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Configurations
{
    public static class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, EditorConfigFile> s_cachedOptions
            = new ConditionalWeakTable<AnalyzerOptions, EditorConfigFile>();

        private static bool TryGetConfigurationValue(this AnalyzerOptions options, string key, out string value)
        {
            var configuration = GetOrComputeEditorConfigFile(options, CancellationToken.None);
            return configuration.TryGetValue(key, out value);
        }

        public static string GetConfigurationValue(this AnalyzerOptions options, string key, string defaultValue)
        {
            if (TryGetConfigurationValue(options, key, out var result))
                return result;

            return defaultValue;
        }

        public static T GetConfigurationValue<T>(this AnalyzerOptions options, string key, T defaultValue)
        {
            if (TryGetConfigurationValue(options, key, out var result))
            {
                try
                {
                    return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        private static EditorConfigFile GetOrComputeEditorConfigFile(this AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists. 
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            var createValueCallback = new ConditionalWeakTable<AnalyzerOptions, EditorConfigFile>.CreateValueCallback(_ => ComputeCategorizedAnalyzerConfigOptions());
            return s_cachedOptions.GetValue(options, createValueCallback);

            // Local functions.
            EditorConfigFile ComputeCategorizedAnalyzerConfigOptions()
            {
                foreach (var additionalFile in options.AdditionalFiles)
                {
                    var fileName = Path.GetFileName(additionalFile.Path);
                    if (fileName.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = additionalFile.GetText(cancellationToken);
                        return EditorConfigFileParser.Parse(text);
                    }
                }

                return EditorConfigFile.Empty;
            }
        }
    }
}
