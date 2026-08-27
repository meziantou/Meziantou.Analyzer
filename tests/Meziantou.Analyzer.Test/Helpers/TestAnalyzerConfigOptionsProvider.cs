using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Helpers;

/// <summary>
/// Exposes the values configured by <see cref="TestHelper.ProjectBuilder.AddAnalyzerConfiguration"/> to the
/// analyzers, falling back to the options computed by the Roslyn test framework for the keys it does not define.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string>? values, AnalyzerConfigOptionsProvider? fallback = null) : AnalyzerConfigOptionsProvider
{
    private readonly Dictionary<string, string> _values = values ?? [with(StringComparer.Ordinal)];

    public override AnalyzerConfigOptions GlobalOptions => Create(fallback?.GlobalOptions);
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Create(fallback?.GetOptions(tree));
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Create(fallback?.GetOptions(textFile));

    private TestAnalyzerConfigOptions Create(AnalyzerConfigOptions? fallbackOptions) => new(_values, fallbackOptions);

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> values, AnalyzerConfigOptions? fallback) : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values = values;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (_values.TryGetValue(key, out value))
                return true;

            if (fallback is not null)
                return fallback.TryGetValue(key, out value);

            value = null;
            return false;
        }
    }
}
