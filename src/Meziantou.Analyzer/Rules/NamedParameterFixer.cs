using System.Collections.Generic;
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

            var parameters = FindParameters(semanticModel, argument);
            if (parameters == null)
                return document;

            var index = NamedParameterAnalyzer.ArgumentIndex(argument);
            if (index < 0 || index >= parameters.Count)
                return document;

            var parameter = parameters[index];
            var argumentName = parameter.Name;

            editor.ReplaceNode(argument, argument.WithNameColon(SyntaxFactory.NameColon(argumentName)));
            return editor.GetChangedDocument();
        }

        private static IReadOnlyList<IParameterSymbol> FindParameters(SemanticModel semanticModel, SyntaxNode node)
        {
            while (node != null)
            {
                switch (node)
                {
                    case InvocationExpressionSyntax invocationExpression:
                        var method = (IMethodSymbol)semanticModel.GetSymbolInfo(invocationExpression).Symbol;
                        return method?.Parameters;

                    case ObjectCreationExpressionSyntax objectCreationExpression:
                        var ctor = (IMethodSymbol)semanticModel.GetSymbolInfo(objectCreationExpression).Symbol;
                        return ctor?.Parameters;
                }

                node = node.Parent;
            }

            return null;
        }
    }
}
