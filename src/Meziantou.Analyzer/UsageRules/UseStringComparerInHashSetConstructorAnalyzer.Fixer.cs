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

namespace Meziantou.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UseStringComparerInHashSetConstructorFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparerInHashSetConstructor);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
            // get the innermost node for ties.
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Add StringComparer.Ordinal";
            var codeAction = CodeAction.Create(
                title,
                ct => AddStringComparer(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> AddStringComparer(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var stringComparer = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparer");
            var newArgument = (ArgumentSyntax)generator.Argument(
                generator.MemberAccessExpression(
                    generator.TypeExpression(stringComparer, addImport: true),
                    nameof(StringComparer.Ordinal)));

            switch (nodeToFix)
            {
                case ObjectCreationExpressionSyntax creationExpression:
                    editor.ReplaceNode(creationExpression, creationExpression.AddArgumentListArguments(newArgument));
                    break;

                case InvocationExpressionSyntax invocationExpression:
                    editor.ReplaceNode(invocationExpression, invocationExpression.AddArgumentListArguments(newArgument));
                    break;

                default:
                    return document;
            }

            return editor.GetChangedDocument();
        }
    }
}
