using System;
using System.Collections.Concurrent;
using System.IO;

namespace Meziantou.Analyzer.Configurations
{
    internal class ConfigurationHierarchy
    {
        private readonly ConcurrentDictionary<string, EditorConfigFile> _configurationFiles = new ConcurrentDictionary<string, EditorConfigFile>(StringComparer.OrdinalIgnoreCase);
        private readonly EditorConfigFile _rootEditorConfigFile;

        public ConfigurationHierarchy(EditorConfigFile rootEditorConfigFile)
        {
            _rootEditorConfigFile = rootEditorConfigFile;
        }

        public bool TryGetValue(string filePath, string key, out string result)
        {
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

            if (_rootEditorConfigFile.TryGetValue(key, out result))
                return true;

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
