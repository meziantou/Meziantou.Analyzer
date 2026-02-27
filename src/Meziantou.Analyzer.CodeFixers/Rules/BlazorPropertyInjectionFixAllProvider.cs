#if CSHARP12_OR_GREATER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer.Rules;

internal sealed class BlazorPropertyInjectionFixAllProvider : FixAllProvider
{
    public static readonly BlazorPropertyInjectionFixAllProvider Instance = new();

    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        var diagnosticsToFix = await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);
        if (diagnosticsToFix.IsEmpty)
            return null;

        return CodeAction.Create(
            "Use constructor injection",
            ct => FixAllAsync(fixAllContext, diagnosticsToFix, ct),
            equivalenceKey: "Use constructor injection");
    }

    private static async Task<Solution> FixAllAsync(FixAllContext fixAllContext, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var solution = fixAllContext.Project.Solution;

        // Group diagnostics by document
        var diagnosticsByDocument = new Dictionary<DocumentId, List<Diagnostic>>();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Location.IsInSource)
            {
                var document = solution.GetDocument(diagnostic.Location.SourceTree);
                if (document is not null)
                {
                    if (!diagnosticsByDocument.TryGetValue(document.Id, out var list))
                    {
                        list = [];
                        diagnosticsByDocument[document.Id] = list;
                    }

                    list.Add(diagnostic);
                }
            }
        }

        // Process each document
        foreach (var (documentId, documentDiagnostics) in diagnosticsByDocument)
        {
            var document = solution.GetDocument(documentId);
            if (document is null)
                continue;

            solution = await BlazorPropertyInjectionShouldUseConstructorInjectionFixer.FixDocumentAsync(
                document,
                ImmutableArray.CreateRange(documentDiagnostics),
                cancellationToken).ConfigureAwait(false);
        }

        return solution;
    }
}
#endif
