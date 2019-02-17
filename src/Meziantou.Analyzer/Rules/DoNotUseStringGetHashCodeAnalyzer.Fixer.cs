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

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class DoNotUseStringGetHashCodeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseStringGetHashCode);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true) as InvocationExpressionSyntax;
            if (nodeToFix == null)
                return;

            if (!(nodeToFix.Expression is MemberAccessExpressionSyntax))
                return;

            var title = "Use StringComparer.Ordinal";
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

            var stringComparer = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparer");
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            var newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(stringComparer, addImport: true),
                        nameof(StringComparer.Ordinal)),
                    nameof(StringComparer.GetHashCode)),
                memberAccessExpression.Expression);

            editor.ReplaceNode(invocationExpression, newExpression);
            return editor.GetChangedDocument();
        }
    }
}
