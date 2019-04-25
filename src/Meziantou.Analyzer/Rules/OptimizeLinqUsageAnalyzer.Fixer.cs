using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class OptimizeLinqUsageFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods);

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

            if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault("Data", ""), out OptimizeLinqUsageData data) || data == OptimizeLinqUsageData.None)
                return;

            var title = "Optimize linq usage";
            switch (data)
            {
                case OptimizeLinqUsageData.UseLengthProperty:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseLengthProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseLongLengthProperty:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseLongLengthProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseCountProperty:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseCountProperty(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseFindMethod:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseFindMethod(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseIndexer:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexer(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseIndexerFirst:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerFirst(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;

                case OptimizeLinqUsageData.UseIndexerLast:
                    context.RegisterCodeFix(CodeAction.Create(title, ct => UseIndexerLast(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                    break;
            }
        }

        private async static Task<Document> UseLengthProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var propertyAccess = generator.MemberAccessExpression(expression, "Length");

            editor.ReplaceNode(nodeToFix, propertyAccess);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseLongLengthProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var propertyAccess = generator.MemberAccessExpression(expression, "LongLength");

            editor.ReplaceNode(nodeToFix, propertyAccess);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseCountProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var propertyAccess = generator.MemberAccessExpression(expression, "Count");

            editor.ReplaceNode(nodeToFix, propertyAccess);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseFindMethod(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetMemberAccessExpression(nodeToFix);
            if (expression == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var newExpression = expression.WithName(SyntaxFactory.IdentifierName("Find"));

            editor.ReplaceNode(expression, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexer(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, operation.Arguments[1].Syntax);

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexerFirst(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax, generator.LiteralExpression(0));

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }

        private async static Task<Document> UseIndexerLast(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var expression = GetParentMemberExpression(nodeToFix);
            if (expression == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(nodeToFix, cancellationToken) as IInvocationOperation;
            if (operation == null)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var newExpression = generator.ElementAccessExpression(operation.Arguments[0].Syntax,
                generator.SubtractExpression(
                    generator.MemberAccessExpression(operation.Arguments[0].Syntax, GetMemberName()),
                    generator.LiteralExpression(1)));

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();

            string GetMemberName()
            {
                var type = operation.Arguments[0].Value.GetActualType();
                var isArray = type != null && type.TypeKind == TypeKind.Array;
                if (isArray)
                    return "Length";

                return "Count";
            }
        }

        private static MemberAccessExpressionSyntax GetMemberAccessExpression(SyntaxNode invocationExpressionSyntax)
        {
            var invocationExpression = invocationExpressionSyntax as InvocationExpressionSyntax;
            if (invocationExpression == null)
                return null;

            return invocationExpression.Expression as MemberAccessExpressionSyntax;
        }

        private static SyntaxNode GetParentMemberExpression(SyntaxNode invocationExpressionSyntax)
        {
            var memberAccessExpression = GetMemberAccessExpression(invocationExpressionSyntax);
            if (memberAccessExpression == null)
                return null;

            return memberAccessExpression.Expression;
        }
    }
}
