using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class OptimizeStringBuilderUsageFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.OptimizeStringBuilderUsage);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault("Data", ""), out OptimizeStringBuilderUsageData data) || data == OptimizeStringBuilderUsageData.None)
                return;

            var title = "Optimize StringBuilder usage";
            switch (data)
            {
                case OptimizeStringBuilderUsageData.RemoveArgument:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveArgument(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeStringBuilderUsageData.RemoveMethod:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveMethod(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeStringBuilderUsageData.ReplaceWithChar:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveMethod(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;
            }
        }

        private static async Task<Document> RemoveArgument(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var newExpression = ((InvocationExpressionSyntax)nodeToFix).WithArgumentList(SyntaxFactory.ArgumentList());

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }

        private static async Task<Document> RemoveMethod(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var newExpression = (InvocationExpressionSyntax)nodeToFix;
            var expression = newExpression.Expression as MemberAccessExpressionSyntax;
            if (expression != null)
            {
                editor.ReplaceNode(nodeToFix, expression.Expression);
            }

            return editor.GetChangedDocument();
        }
    }
}
