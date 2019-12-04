using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UseGuidEmptyFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseGuidEmpty);

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

            var title = "Use Guid.Empty";
            var codeAction = CodeAction.Create(
                title,
                ct => ConvertToArrayEmpty(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> ConvertToArrayEmpty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            editor.ReplaceNode(nodeToFix, GenerateArrayEmptyInvocation(generator, semanticModel).WithTriviaFrom(nodeToFix));
            return editor.GetChangedDocument();
        }

        private static SyntaxNode GenerateArrayEmptyInvocation(SyntaxGenerator generator, SemanticModel semanticModel)
        {
            return generator.MemberAccessExpression(
                generator.TypeExpression(semanticModel.Compilation.GetTypeByMetadataName("System.Guid")),
                nameof(Guid.Empty));
        }
    }
}
