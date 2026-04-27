using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal sealed class UsePatternMatchingForEqualityComparisonsFixAllProvider : DocumentBasedFixAllProvider
{
    public static UsePatternMatchingForEqualityComparisonsFixAllProvider Instance { get; } = new();

    protected override string GetFixAllTitle(FixAllContext fixAllContext) => "Use pattern matching";

    protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.IsEmpty)
            return null;

        var newDocument = await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
        return newDocument;
    }

    private static async Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics.OrderByDescending(diagnostic => diagnostic.Location.SourceSpan.Start))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (nodeToFix is not BinaryExpressionSyntax binaryExpression)
                continue;

            document = await UsePatternMatchingForEqualityComparisonsFixer.UpdateDocumentAsync(document, binaryExpression, cancellationToken).ConfigureAwait(false);
        }

        return document;
    }
}
