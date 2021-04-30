using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
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
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var diagnostic = context.Diagnostics[0];

            var title = "Remove comparison with bool constant";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveComparisonWithBoolConstant(context.Document, diagnostic, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> RemoveComparisonWithBoolConstant(Document document, Diagnostic diagnostic, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            if (nodeToFix is not BinaryExpressionSyntax binaryExpressionSyntax)
                return document;

            if (binaryExpressionSyntax.Left is null || binaryExpressionSyntax.Right is null)
                return document;

            var nodeToKeepSpanStart = int.Parse(diagnostic.Properties["NodeToKeepSpanStart"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var nodeToKeepSpanLength = int.Parse(diagnostic.Properties["NodeToKeepSpanLength"]!, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var logicalNotOperatorNeeded = bool.Parse(diagnostic.Properties["LogicalNotOperatorNeeded"]!);

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot == null)
                return document;

            var nodeToKeep = syntaxRoot.FindNode(new TextSpan(nodeToKeepSpanStart, nodeToKeepSpanLength), getInnermostNodeForTie: true);
            if (nodeToKeep.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
                nodeToKeep = nodeToKeep.Parent;

            if (logicalNotOperatorNeeded)
            {
                nodeToKeep = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, (ExpressionSyntax)nodeToKeep);
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(nodeToFix, nodeToKeep.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));

            return editor.GetChangedDocument();
        }
    }
}
