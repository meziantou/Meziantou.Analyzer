using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Helpers;

internal sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string>? values) : AnalyzerConfigOptionsProvider
{
    private readonly Dictionary<string, string> _values = values ?? [];

    public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_values);
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(_values);
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(_values);

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values = values;

        public override bool TryGetValue(string key, [NotNullWhen(true)]out string? value)
        {
            return _values.TryGetValue(key, out value);
        }
    }
}
