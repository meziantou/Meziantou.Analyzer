using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
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

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Remove redundant else";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveRedundantElse(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
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

            // If ifStatementParent is not a BlockSyntax, we need to create one to be able to split 'if' and 'else',
            // and insert the 'else' child nodes after the 'if' statement.
            if (!(ifStatementParent is BlockSyntax blockSyntax))
            {
                blockSyntax = Block(new[]
                {
                    modifiedIfStatement
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
