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
using static Meziantou.Analyzer.Rules.AvoidUsingRedundantElseAnalyzer;
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
            foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan))
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
            if (!(nodeToFix is ElseClauseSyntax elseClause))
                return document;

            if (!(elseClause.Parent is IfStatementSyntax ifStatement))
                return document;

            var ifStatementParent = ifStatement.Parent;
            if (ifStatementParent is null)
                return document;

            // Get all syntax nodes currently under the 'else' clause
            var nodesAfterNewIfStatement = GetElseClauseChildren(elseClause)
                .Select(n => n.WithAdditionalAnnotations(Formatter.Annotation))
                .ToArray();

            var newIfStatementTrailingTrivia = nodesAfterNewIfStatement.Length > 0 ?
                ifStatement.GetTrailingTrivia().Add(LineFeed) :
                ifStatement.GetTrailingTrivia();

            var newIfStatement = ifStatement
                .WithElse(null)
                .WithTrailingTrivia(newIfStatementTrailingTrivia);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // If ifStatementParent is not a Block, we need to create one to be able to split 'if' and 'else',
            // and insert the 'else' child nodes after the 'if' statement.
            if (!ifStatementParent.IsKind(SyntaxKind.Block))
            {
                var surroundingBlock = Block(new[]
                {
                    newIfStatement,
                }.Concat(nodesAfterNewIfStatement.Cast<StatementSyntax>()));
                editor.ReplaceNode(ifStatement, surroundingBlock);
            }
            else
            {
                editor.InsertAfter(ifStatement, nodesAfterNewIfStatement);
                editor.ReplaceNode(ifStatement, newIfStatement);
            }

            return editor.GetChangedDocument();
        }
    }
}
