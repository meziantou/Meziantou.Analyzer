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
    public sealed class UseStringComparerFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparer);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
            // get the innermost node for ties.
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            RegisterCodeFix(nameof(StringComparer.Ordinal));
            RegisterCodeFix(nameof(StringComparer.OrdinalIgnoreCase));

            void RegisterCodeFix(string comparerName)
            {
                var title = "Add StringComparer." + comparerName;
                var codeAction = CodeAction.Create(
                    title,
                    ct => AddStringComparer(context.Document, nodeToFix, comparerName, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static async Task<Document> AddStringComparer(Document document, SyntaxNode nodeToFix, string comparerName, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var semanticModel = editor.SemanticModel;

            var stringComparer = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparer");
            var newArgument = (ArgumentSyntax)generator.Argument(
                generator.MemberAccessExpression(
                    generator.TypeExpression(stringComparer, addImport: true),
                    comparerName));

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
