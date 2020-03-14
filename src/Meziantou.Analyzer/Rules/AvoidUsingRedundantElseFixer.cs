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

            var diagnostic = context.Diagnostics[0];

            var title = "Remove redundant 'else'";
            var codeAction = CodeAction.Create(
                title,
                ct => Refactor(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (!(nodeToFix is ElseClauseSyntax elseClauseSyntax))
                return document;

            if (!(elseClauseSyntax.Parent is IfStatementSyntax ifStatementSyntax))
                return document;

            var elseChildNodes = (elseClauseSyntax.Statement is BlockSyntax elseBlockSyntax) ?
                elseBlockSyntax.ChildNodes() :
                elseClauseSyntax.ChildNodes();

            // For now, limit the fix to the case where ifStatementSyntax is child of a blockSyntax
            if (!(ifStatementSyntax.Parent is BlockSyntax blockSyntax))
                return document;

            var nodes = elseChildNodes.Select(n => n.WithAdditionalAnnotations(Formatter.Annotation));
            editor.InsertAfter(ifStatementSyntax, nodes);

            editor.ReplaceNode(ifStatementSyntax, ifStatementSyntax
                .WithElse(null)
                .WithTrailingTrivia(ifStatementSyntax.GetTrailingTrivia().Add(LineFeed)));

            var newDoc = editor.GetChangedDocument();

            var text = await newDoc.GetTextAsync().ConfigureAwait(false);

            return editor.GetChangedDocument();
        }
    }
}
