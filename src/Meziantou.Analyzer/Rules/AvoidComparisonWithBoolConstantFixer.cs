using System.Collections.Immutable;
using System.Composition;
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
    public sealed class AvoidComparisonWithBoolConstantFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AvoidComparisonWithBoolConstant);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Remove comparison with bool literal";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveComparisonWithBoolConstant(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> RemoveComparisonWithBoolConstant(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            if (!(nodeToFix is BinaryExpressionSyntax binaryExpressionSyntax))
                return document;

            if (binaryExpressionSyntax.Left is null || binaryExpressionSyntax.Right is null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var leftConstant = semanticModel.GetConstantValue(binaryExpressionSyntax.Left);
            var rightConstant = semanticModel.GetConstantValue(binaryExpressionSyntax.Right);

            ExpressionSyntax fixedNode;
            bool invertCondition;
            if (leftConstant.HasValue)
            {
                fixedNode = binaryExpressionSyntax.Right;
                invertCondition = (bool)leftConstant.Value ?
                    binaryExpressionSyntax.IsKind(SyntaxKind.NotEqualsExpression) :
                    binaryExpressionSyntax.IsKind(SyntaxKind.EqualsExpression);
            }
            else if (rightConstant.HasValue)
            {
                fixedNode = binaryExpressionSyntax.Left;
                invertCondition = (bool)rightConstant.Value ?
                    binaryExpressionSyntax.IsKind(SyntaxKind.NotEqualsExpression) :
                    binaryExpressionSyntax.IsKind(SyntaxKind.EqualsExpression);
            }
            else
            {
                return document;
            }

            if (invertCondition)
            {
                fixedNode = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, fixedNode);
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(nodeToFix, fixedNode
                .WithAdditionalAnnotations(Formatter.Annotation));

            return editor.GetChangedDocument();
        }
    }
}
