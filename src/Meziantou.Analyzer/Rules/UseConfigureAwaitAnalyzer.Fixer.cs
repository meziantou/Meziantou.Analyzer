using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UseConfigureAwaitFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseConfigureAwaitFalse);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
            // get the innermost node for ties.
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Use ConfigureAwait(false)";
            var codeAction = CodeAction.Create(
                title,
                ct => AddConfigureAwait(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> AddConfigureAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var syntax = (AwaitExpressionSyntax)nodeToFix;
            if (syntax == null || syntax.Expression == null)
                return document;

            var newExpression = (ExpressionSyntax)generator.InvocationExpression(
                generator.MemberAccessExpression(syntax.Expression, nameof(Task.ConfigureAwait)),
                generator.FalseLiteralExpression());

            var newInvokeExpression = syntax.WithExpression(newExpression);

            editor.ReplaceNode(nodeToFix, newInvokeExpression);
            return editor.GetChangedDocument();
        }
    }
}
