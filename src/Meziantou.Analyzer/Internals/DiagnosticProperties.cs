using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class DiagnosticProperties
    {
        private const string PropertyKey = "Data";

        public static T Get<T>(Diagnostic diagnostic)
        {
            if (diagnostic.Properties.TryGetValue(PropertyKey, out var value))
            {
                return SimpleJson.SimpleJson.DeserializeObject<T>(value);
            }

            return default;
        }

        public static ImmutableDictionary<string, string> Create(object value)
        {
            return ImmutableDictionary.Create<string, string>()
                .Add(PropertyKey, SimpleJson.SimpleJson.SerializeObject(value));
        }
    }
}
