using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Meziantou.Analyzer.Configurations
{
    internal class EditorConfigFile
    {
        private readonly IReadOnlyDictionary<string, string> _configurations;

        public static EditorConfigFile Empty { get; } = new EditorConfigFile(ImmutableDictionary<string, string>.Empty);

        public EditorConfigFile(IReadOnlyDictionary<string, string> configurations)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
        }

        public bool TryGetValue(string key, out string result)
        {
            return _configurations.TryGetValue(key, out result);
        }

        public bool IsRoot => TryGetValue("root", out var value) && bool.TryParse(value, out var result) && result;
    }
}
