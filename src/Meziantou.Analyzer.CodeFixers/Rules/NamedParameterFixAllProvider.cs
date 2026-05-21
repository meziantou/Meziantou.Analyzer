using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer.Rules;

internal sealed class NamedParameterFixAllProvider : DocumentBasedFixAllProvider
{
    public static NamedParameterFixAllProvider Instance { get; } = new();

    protected override string GetFixAllTitle(FixAllContext fixAllContext) => "Add parameter name";

    protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.IsEmpty)
            return null;

        foreach (var diagnostic in diagnostics.OrderByDescending(diagnostic => diagnostic.Location.SourceSpan.Start))
        {
            document = await NamedParameterFixer.AddParameterName(document, diagnostic.Location.SourceSpan, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        return document;
    }
}
