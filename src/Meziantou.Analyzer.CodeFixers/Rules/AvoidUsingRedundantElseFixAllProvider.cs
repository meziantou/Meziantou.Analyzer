using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer.Rules;

internal sealed class AvoidUsingRedundantElseFixAllProvider : DocumentBasedFixAllProvider
{
    public static AvoidUsingRedundantElseFixAllProvider Instance { get; } = new AvoidUsingRedundantElseFixAllProvider();

    protected override string GetFixAllTitle(FixAllContext fixAllContext) => "Remove redundant else";

    protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.IsEmpty)
            return null;

        var newDocument = await AvoidUsingRedundantElseFixer.RemoveRedundantElseClausesInDocument(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
        return newDocument;
    }
}
