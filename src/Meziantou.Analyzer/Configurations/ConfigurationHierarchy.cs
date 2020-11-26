using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Meziantou.Analyzer.Configurations
{
    internal sealed class ConfigurationHierarchy
    {
        private readonly ConcurrentDictionary<string, EditorConfigFile> _configurationFiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly EditorConfigFile _projectEditorConfigFile;

        public ConfigurationHierarchy(EditorConfigFile rootEditorConfigFile)
        {
            _projectEditorConfigFile = rootEditorConfigFile;
        }

        public bool TryGetValue(string filePath, string key, [NotNullWhen(true)] out string? result)
        {
            if (_projectEditorConfigFile.TryGetValue(key, out result))
                return true;

            var directory = Path.GetDirectoryName(filePath);
            while (!string.IsNullOrEmpty(directory))
            {
                var configurationFile = LoadForDirectory(directory);
                if (configurationFile.TryGetValue(key, out result))
                    return true;

                if (configurationFile.IsRoot)
                    break;

                directory = Path.GetDirectoryName(directory);
            }

            result = default;
            return false;
        }

        private EditorConfigFile LoadForDirectory(string folderPath)
        {
            return _configurationFiles.GetOrAdd(folderPath, path =>
            {
                var editorConfigurationPath = Path.Combine(path, ".editorconfig");
                return EditorConfigFileParser.Load(editorConfigurationPath);
            });
        }
    }
}
