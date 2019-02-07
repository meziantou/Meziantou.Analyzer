using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.UsageRules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UseStringComparisonFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparison);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
            // get the innermost node for ties.
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Add StringComparison.Ordinal";
            var codeAction = CodeAction.Create(
                title,
                ct => AddStringComparison(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> AddStringComparison(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var invocationExpression = (InvocationExpressionSyntax)nodeToFix;
            if (invocationExpression == null)
                return document;

            var stringComparison = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparison");
            var newArgument = (ArgumentSyntax)generator.Argument(
                generator.MemberAccessExpression(
                    generator.TypeExpression(stringComparison, addImport: true),
                    nameof(StringComparison.Ordinal)));

            editor.ReplaceNode(invocationExpression, invocationExpression.AddArgumentListArguments(newArgument));
            return editor.GetChangedDocument();
        }
    }
}
