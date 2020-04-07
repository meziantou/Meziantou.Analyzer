using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class AvoidUsingRedundantElseFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AvoidUsingRedundantElse);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            // We can't use WellKnownFixAllProviders.BatchFixer here, as we need to fix diagnostics per document in reverse order
            return AvoidUsingRedundantElseFixAllProvider.Instance;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var title = "Remove redundant else";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveRedundantElseClausesInDocument(context.Document, context.Diagnostics, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);

            return Task.CompletedTask;
        }

        internal static async Task<Document> RemoveRedundantElseClausesInDocument(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics.Reverse())
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (nodeToFix == null)
                    continue;

                document = await RemoveRedundantElse(document, nodeToFix, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private static async Task<Document> RemoveRedundantElse(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            if (!(nodeToFix is ElseClauseSyntax elseClauseSyntax))
                return document;

            if (!(elseClauseSyntax.Parent is IfStatementSyntax ifStatementSyntax))
                return document;

            var ifStatementParent = ifStatementSyntax.Parent;
            if (ifStatementParent is null)
                return document;

            // Get all syntax nodes currently under the 'else' clause
            var elseChildNodes = (elseClauseSyntax.Statement is BlockSyntax elseBlockSyntax) ?
                elseBlockSyntax.ChildNodes() :
                new[] { elseClauseSyntax.Statement };

            var formattedElseChildNodes = elseChildNodes.Select(n => n.WithAdditionalAnnotations(Formatter.Annotation)).ToArray();

            var ifTrailingTrivia = formattedElseChildNodes.Length > 0 ?
                ifStatementSyntax.GetTrailingTrivia().Add(LineFeed) :
                ifStatementSyntax.GetTrailingTrivia();

            var modifiedIfStatement = ifStatementSyntax
                .WithElse(null)
                .WithTrailingTrivia(ifTrailingTrivia);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // If ifStatementParent is not a Block, we need to create one to be able to split 'if' and 'else',
            // and insert the 'else' child nodes after the 'if' statement.
            if (!ifStatementParent.IsKind(SyntaxKind.Block))
            {
                var blockSyntax = Block(new[]
                {
                    modifiedIfStatement,
                }.Concat(formattedElseChildNodes.Cast<StatementSyntax>()));
                editor.ReplaceNode(ifStatementSyntax, blockSyntax);
            }
            else
            {
                editor.InsertAfter(ifStatementSyntax, formattedElseChildNodes);
                editor.ReplaceNode(ifStatementSyntax, modifiedIfStatement);
            }

            return editor.GetChangedDocument();
        }
    }
}
