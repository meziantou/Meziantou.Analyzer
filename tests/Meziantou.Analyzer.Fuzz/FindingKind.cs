using System.Text.Json.Serialization;

namespace Meziantou.Analyzer.Fuzz;

[JsonConverter(typeof(JsonStringEnumConverter<FindingKind>))]
internal enum FindingKind
{
    /// <summary>An analyzer or a diagnostic suppressor throws (AD0001).</summary>
    AnalyzerException,

    /// <summary>A code fixer throws when it registers its code fixes, or when one of them is computed or applied.</summary>
    CodeFixException,

    /// <summary>The input compiles without errors, but a code fix introduces compiler errors.</summary>
    CodeFixBreaksCode,

    /// <summary>A code fixer registers a code fix that does not change the code.</summary>
    CodeFixDoesNotChangeCode,

    /// <summary>The process crashes, e.g. on a stack overflow.</summary>
    Crash,

    /// <summary>An analyzer or a code fixer does not complete.</summary>
    Timeout,

    /// <summary>
    /// Roslyn throws while it runs an analyzer or applies a code fix, and no frame of the analyzer assemblies is in the stack trace,
    /// e.g. the formatter fails on the invalid code of the input. These are not reported, as they are rarely defects of the rules.
    /// </summary>
    RoslynException,

    /// <summary>The harness itself or Roslyn throws, outside of an analyzer or a code fixer.</summary>
    HarnessException,
}
