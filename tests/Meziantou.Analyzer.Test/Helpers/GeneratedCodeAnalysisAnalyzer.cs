// This is a test helper, not a compiler extension that ships to users, so the analyzer authoring rules do not
// apply: the wrapped analyzer is the one that calls EnableConcurrentExecution (RS1026), and the assembly is
// never loaded during a command line compilation (RS1038)
#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1038 // This compiler extension should not be implemented in an assembly containing a reference to Microsoft.CodeAnalysis.Workspaces

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Helpers;

/// <summary>
/// Wraps an analyzer to override how it handles generated code. This is how the tests simulate the
/// <c>MEZIANTOU_ANALYZER_GENERATED_CODE</c> environment variable without mutating the state of the process,
/// as the tests run in parallel. <see cref="AnalysisContext.ConfigureGeneratedCodeAnalysis"/> is last-call-wins.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedCodeAnalysisAnalyzer(DiagnosticAnalyzer inner, GeneratedCodeAnalysisFlags flags) : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => inner.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        inner.Initialize(context);
        context.ConfigureGeneratedCodeAnalysis(flags);
    }
}
