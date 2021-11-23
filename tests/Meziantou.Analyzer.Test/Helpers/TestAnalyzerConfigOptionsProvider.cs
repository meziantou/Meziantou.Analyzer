using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Helpers;

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly Dictionary<string, string> _values;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> values)
    {
        _values = values ?? new Dictionary<string, string>();
    }

    public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_values);
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(_values);
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(_values);

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        public TestAnalyzerConfigOptions(Dictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value);
        }
    }
}
