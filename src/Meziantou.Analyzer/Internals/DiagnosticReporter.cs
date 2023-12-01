using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer;

internal readonly struct DiagnosticReporter
{
    private readonly Action<Diagnostic> _reportDiagnostic;

    public DiagnosticReporter(SymbolAnalysisContext context)
    {
        _reportDiagnostic = context.ReportDiagnostic;
        CancellationToken = context.CancellationToken;
    }

    public DiagnosticReporter(OperationAnalysisContext context)
    {
        _reportDiagnostic = context.ReportDiagnostic;
        CancellationToken = context.CancellationToken;
    }

    public DiagnosticReporter(OperationBlockAnalysisContext context)
    {
        _reportDiagnostic = context.ReportDiagnostic;
        CancellationToken = context.CancellationToken;
    }

    public DiagnosticReporter(SyntaxNodeAnalysisContext context)
    {
        _reportDiagnostic = context.ReportDiagnostic;
        CancellationToken = context.CancellationToken;
    }

    public DiagnosticReporter(CompilationAnalysisContext context)
    {
        _reportDiagnostic = context.ReportDiagnostic;
        CancellationToken = context.CancellationToken;
    }

    public CancellationToken CancellationToken { get; }

    public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);
}
