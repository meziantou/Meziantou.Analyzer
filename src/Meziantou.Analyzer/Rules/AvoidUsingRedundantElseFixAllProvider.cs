using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Meziantou.Analyzer.Rules
{
    internal sealed class AvoidUsingRedundantElseFixAllProvider : DocumentBasedFixAllProvider
    {
        public static AvoidUsingRedundantElseFixAllProvider Instance { get; } = new AvoidUsingRedundantElseFixAllProvider();

        protected override string CodeActionTitle => "Remove redundant else";

        /// <inheritdoc/>
        protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsEmpty)
                return null;

            var newDocument = await AvoidUsingRedundantElseFixer.RemoveRedundantElseClausesInDocument(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);

            return await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);
        }
    }
}
