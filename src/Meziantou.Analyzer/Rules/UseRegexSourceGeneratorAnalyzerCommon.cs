using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal static class UseRegexSourceGeneratorAnalyzerCommon
{
    internal const string PatternIndexName = "PatternIndex";
    internal const string RegexOptionsIndexName = "RegexOptionsIndex";
    internal const string RegexTimeoutIndexName = "RegexTimeoutIndex";
    internal const string RegexTimeoutName = "RegexTimeout";
}
