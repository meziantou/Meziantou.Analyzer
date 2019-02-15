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

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class NamedParameterFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseNamedParameter);

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

            var title = "Add parameter name";
            var codeAction = CodeAction.Create(
                title,
                ct => AddParameterName(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> AddParameterName(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var argument = nodeToFix.FirstAncestorOrSelf<ArgumentSyntax>();
            if (argument == null || argument.NameColon != null)
                return document;

            var invocationExpression = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocationExpression == null)
                return document;

            var methodSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocationExpression).Symbol;
            if (methodSymbol == null)
                return document;

            var index = NamedParameterAnalyzer.ArgumentIndex(argument);
            if (index < 0 || index >= methodSymbol.Parameters.Length)
                return document;

            var parameter = methodSymbol.Parameters[index];
            var argumentName = parameter.Name;

            editor.ReplaceNode(argument, argument.WithNameColon(SyntaxFactory.NameColon(argumentName)));
            return editor.GetChangedDocument();
        }
    }
}
